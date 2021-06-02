﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.ServiceBus;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Arcus.Messaging.Pumps.Abstractions;
using Arcus.Messaging.Pumps.Abstractions.Telemetry;
using Arcus.Messaging.Pumps.ServiceBus.Configuration;
using Arcus.Observability.Correlation;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using GuardNet;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace Arcus.Messaging.Pumps.ServiceBus
{
    /// <summary>
    ///     Message pump for processing messages on an Azure Service Bus entity
    /// </summary>
    public class AzureServiceBusMessagePump : MessagePump
    {
        private readonly IAzureServiceBusMessageRouter _messageRouter;
        private readonly ServiceBusProcessorOptions _messageProcessorOptions;
        private readonly IDisposable _loggingScope;
        
        private bool _isHostShuttingDown;
        private ServiceBusProcessor _messageProcessor;
        private int _unauthorizedExceptionCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureServiceBusMessagePump"/> class.
        /// </summary>
        /// <param name="settings">Settings to configure the message pump</param>
        /// <param name="applicationConfiguration">Configuration of the application</param>
        /// <param name="serviceProvider">Collection of services that are configured</param>
        /// <param name="messageRouter">The router to route incoming Azure Service Bus messages through registered <see cref="IAzureServiceBusMessageHandler{TMessage}"/>s.</param>
        /// <param name="logger">Logger to write telemetry to</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="settings"/>, <paramref name="settings"/>, <paramref name="serviceProvider"/>, <paramref name="messageRouter"/> is <c>null</c>.</exception>
        public AzureServiceBusMessagePump(
            AzureServiceBusMessagePumpSettings settings,
            IConfiguration applicationConfiguration, 
            IServiceProvider serviceProvider, 
            IAzureServiceBusMessageRouter messageRouter,
            ILogger<AzureServiceBusMessagePump> logger)
            : base(applicationConfiguration, serviceProvider, logger)
        {
            Guard.NotNull(settings, nameof(settings), "Requires a set of settings to correctly configure the message pump");
            Guard.NotNull(applicationConfiguration, nameof(applicationConfiguration), "Requires a configuration instance to retrieve application-specific information");
            Guard.NotNull(serviceProvider, nameof(serviceProvider), "Requires a service provider to retrieve the registered message handlers");
            Guard.NotNull(messageRouter, nameof(messageRouter), "Requires a message router to route incoming Azure Service Bus messages through registered message handlers");
            
            Settings = settings;
            JobId = Settings.Options.JobId;
            SubscriptionName = Settings.SubscriptionName;

            _messageRouter = messageRouter;
            _messageProcessorOptions = DetermineMessageProcessorOptions(Settings);
            _loggingScope = logger.BeginScope("Job: {JobId}", JobId);
        }

        /// <summary>
        ///     Gets the settings configuring the message pump.
        /// </summary>
        public AzureServiceBusMessagePumpSettings Settings { get; }

        /// <summary>
        ///     Service Bus namespace that contains the entity
        /// </summary>
        public string Namespace { get; private set; }

        /// <summary>
        /// Gets the unique identifier for this background job to distinguish this job instance in a multi-instance deployment.
        /// </summary>
        public string JobId { get; }

        /// <summary>
        /// Gets the name of the topic subscription; combined from the <see cref="AzureServiceBusMessagePumpSettings.SubscriptionName"/> and the <see cref="JobId"/>.
        /// </summary>
        protected string SubscriptionName { get; }

        /// <summary>
        /// Reconfigure the Azure Service Bus options on this message pump.
        /// </summary>
        /// <param name="reconfigure">The function to reconfigure the Azure Service Bus options.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="reconfigure"/> is <c>null</c>.</exception>
        public void ReconfigureOptions(Action<AzureServiceBusMessagePumpOptions> reconfigure)
        {
            Guard.NotNull(reconfigure, nameof(reconfigure), "Requires a function to reconfigure the Azure Service Bus options");

            reconfigure(Settings.Options);
        }

        /// <summary>
        /// Reconfigure the Azure Service Bus Queue options on this message pump.
        /// </summary>
        /// <param name="reconfigure">The function to reconfigure the Azure Service Bus Queue options.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="reconfigure"/> is <c>null</c>.</exception>
        /// <exception cref="NotSupportedException">Thrown when the message pump is not configured for Queues.</exception>
        public void ReconfigureQueueOptions(Action<IAzureServiceBusQueueMessagePumpOptions> reconfigure)
        {
            Guard.NotNull(reconfigure, nameof(reconfigure), "Requires a function to reconfigure the Azure Service Bus Queue options");
            Guard.For<NotSupportedException>(
                () => Settings.ServiceBusEntity is ServiceBusEntityType.Topic, 
                "Requires the message pump to be configured for Azure Service Bus Queue to reconfigure these options, use the Topic overload instead");

            reconfigure(Settings.Options);
        }

        /// <summary>
        /// Reconfigure the Azure Service Bus Topic options on this message pump.
        /// </summary>
        /// <param name="reconfigure">The function to reconfigure the Azure Service Bus Topic options.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="reconfigure"/> is <c>null</c>.</exception>
        /// <exception cref="NotSupportedException">Thrown when the message pump is not configured for Topics.</exception>
        public void ReconfigureTopicOptions(Action<IAzureServiceBusTopicMessagePumpOptions> reconfigure)
        {
            Guard.NotNull(reconfigure, nameof(reconfigure), "Requires a function to reconfigure the Azure Service Bus Topics options");
            Guard.For<NotSupportedException>(
                () => Settings.ServiceBusEntity is ServiceBusEntityType.Queue,
                "Requires a message pump to be configured for Azure Service Bus Topic to reconfigure these options, use the Queue overload instead");

            reconfigure(Settings.Options);
        }

        /// <summary>
        /// Triggered when the application host is ready to start the service.
        /// </summary>
        /// <param name="cancellationToken">Indicates that the start process has been aborted.</param>
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            if (Settings.ServiceBusEntity == ServiceBusEntityType.Topic
                && Settings.Options.TopicSubscription.HasValue
                && Settings.Options.TopicSubscription.Value.HasFlag(TopicSubscription.CreateOnStart))
            {
                await CreateTopicSubscriptionAsync(cancellationToken);
            }

            await base.StartAsync(cancellationToken);
        }

        private async Task CreateTopicSubscriptionAsync(CancellationToken cancellationToken)
        {
            string serviceBusConnectionString = await GetServiceBusConnectionStringAsync();
            var serviceBusClient = new ServiceBusAdministrationClient(serviceBusConnectionString);
            var serviceBusConnectionProperties = ServiceBusConnectionStringProperties.Parse(serviceBusConnectionString);

            try
            {
                bool subscriptionExists = await serviceBusClient.SubscriptionExistsAsync(serviceBusConnectionProperties.EntityPath, SubscriptionName, cancellationToken);
                if (subscriptionExists)
                {
                    Logger.LogTrace("Topic subscription with name '{SubscriptionName}' already exists on Service Bus resource", SubscriptionName);
                }
                else
                {
                    Logger.LogTrace("Creating subscription '{SubscriptionName}' on topic '{TopicPath}'...", SubscriptionName, serviceBusConnectionProperties.EntityPath);
                    
                    var subscriptionDescription = new CreateSubscriptionOptions(serviceBusConnectionProperties.EntityPath, SubscriptionName)
                    {
                        UserMetadata = $"Subscription created by Arcus job: '{JobId}' to process Service Bus messages."
                    };
                    var ruleDescription = new CreateRuleOptions("Accept-All", new TrueRuleFilter());
                    await serviceBusClient.CreateSubscriptionAsync(subscriptionDescription, ruleDescription, cancellationToken)
                                          .ConfigureAwait(continueOnCapturedContext: false);

                    Logger.LogTrace("Subscription '{SubscriptionName}' created on topic '{TopicPath}'", SubscriptionName, serviceBusConnectionProperties.EntityPath);
                }
            }
            catch (Exception exception)
            {
                Logger.LogWarning(exception, "Failed to create topic subscription with name '{SubscriptionName}' on Service Bus resource", SubscriptionName);
            }
        }

        /// <inheritdoc />
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await OpenNewMessageReceiverAsync();
                await UntilCancelledAsync(stoppingToken);
            }
            catch (Exception exception)
            {
                Logger.LogCritical(exception, "Unexpected failure occurred during processing of messages");
                await HandleReceiveExceptionAsync(exception);
            }
            finally
            {
                await CloseMessageReceiverAsync();
            }
        }

        private async Task OpenNewMessageReceiverAsync()
        {
            _messageProcessor = await CreateMessageProcessorAsync(Settings);

            Logger.LogTrace("Starting message pump '{JobId}' on entity path '{EntityPath}' in namespace '{Namespace}'", JobId, EntityPath, Namespace);
            _messageProcessor.ProcessErrorAsync += ProcessErrorAsync;
            _messageProcessor.ProcessMessageAsync += ProcessMessageAsync;
            await _messageProcessor.StartProcessingAsync();
            Logger.LogInformation("Message pump '{JobId}' on entity path '{EntityPath}' in namespace '{Namespace}' started", JobId, EntityPath, Namespace);
        }

        private async Task CloseMessageReceiverAsync()
        {
            if (_messageProcessor is null)
            {
                return;
            }

            try
            {
                Logger.LogTrace("Closing message pump '{JobId}' on entity path '{EntityPath}' in '{Namespace}'",  JobId, EntityPath, Namespace);
                await _messageProcessor.StopProcessingAsync();
                _messageProcessor.ProcessMessageAsync -= ProcessMessageAsync;
                _messageProcessor.ProcessErrorAsync -= ProcessErrorAsync;
                await _messageProcessor.CloseAsync();
                Logger.LogInformation("Message pump '{JobId}' on entity path '{EntityPath}' in '{Namespace}' closed : {Time}",  JobId, EntityPath, Namespace, DateTimeOffset.UtcNow);
            }
            catch (TaskCanceledException) 
            {
                // Ignore.
            } 
            catch (Exception exception)
            {
                Logger.LogWarning(exception, "Cannot correctly close the message pump '{JobId}' on entity path '{EntityPath}' in '{Namespace}': {Message}",  JobId, EntityPath, Namespace, exception.Message);
            }
        }

        private ServiceBusProcessorOptions DetermineMessageProcessorOptions(AzureServiceBusMessagePumpSettings messagePumpSettings)
        {
            var messageHandlerOptions = new ServiceBusProcessorOptions();
            if (messagePumpSettings.Options != null)
            {
                // Assign the configured defaults
                messageHandlerOptions.AutoCompleteMessages = messagePumpSettings.Options.AutoComplete;
                messageHandlerOptions.MaxConcurrentCalls = messagePumpSettings.Options.MaxConcurrentCalls ?? messageHandlerOptions.MaxConcurrentCalls;

                Logger.LogTrace("Message pump options were configured instead of Azure Service Bus defaults");
            }
            else
            {
                Logger.LogTrace("No message pump options were configured, using Azure Service Bus defaults instead");
            }

            return messageHandlerOptions;
        }

        private async Task ProcessErrorAsync(ProcessErrorEventArgs args)
        {
            if (args?.Exception is null)
            {
                Logger.LogWarning("Thrown exception on Azure Service Bus message pump '{JobId}' was null, skipping", JobId);
                return;
            }
            
            try
            {
                await HandleReceiveExceptionAsync(args.Exception);
            }
            finally
            {
                if (args.Exception is UnauthorizedAccessException)
                {
                    if (Interlocked.Increment(ref _unauthorizedExceptionCount) >= Settings.Options.MaximumUnauthorizedExceptionsBeforeRestart)
                    {
                        Logger.LogTrace("Unable to connect anymore to Azure Service Bus, trying to re-authenticate...");
                        await RestartAsync();
                    }
                    else
                    {
                        Logger.LogWarning("Unable to connect anymore to Azure Service Bus ({CurrentCount}/{MaxCount})", 
                            _unauthorizedExceptionCount, Settings.Options.MaximumUnauthorizedExceptionsBeforeRestart);
                    }
                }
            }
        }

        /// <summary>
        /// Restart core functionality of the message pump.
        /// </summary>
        public async Task RestartAsync()
        {
            Interlocked.Exchange(ref _unauthorizedExceptionCount, 0);

            Logger.LogTrace("Restarting Azure Service Bus message pump '{JobId}' on entity path '{EntityPath}' in '{Namespace}' ...", JobId, EntityPath, Namespace);
            await CloseMessageReceiverAsync();
            await OpenNewMessageReceiverAsync();
            Logger.LogInformation("Azure Service Bus message pump '{JobId}' on entity path '{EntityPath}' in '{Namespace}' restarted!", JobId, EntityPath, Namespace);
        }

        private async Task<ServiceBusProcessor> CreateMessageProcessorAsync(AzureServiceBusMessagePumpSettings messagePumpSettings)
        {
            string rawConnectionString = await messagePumpSettings.GetConnectionStringAsync();
            ServiceBusConnectionStringProperties serviceBusConnectionString = ServiceBusConnectionStringProperties.Parse(rawConnectionString);

            var client = new ServiceBusClient(rawConnectionString);
            {
                ServiceBusProcessor processor;
                if (string.IsNullOrWhiteSpace(serviceBusConnectionString.EntityPath))
                {
                    // Connection string doesn't include the entity so we're using the message pump settings
                    if (string.IsNullOrWhiteSpace(messagePumpSettings.EntityName))
                    {
                        throw new ArgumentException("No entity name was specified while the connection string is scoped to the namespace");
                    }

                    processor = CreateProcessor(client, messagePumpSettings.EntityName, SubscriptionName);
                }
                else
                {
                    // Connection string includes the entity so we're using that instead of the message pump settings
                    processor = CreateProcessor(client, serviceBusConnectionString.EntityPath, SubscriptionName);
                }

                Namespace = serviceBusConnectionString.Endpoint?.Host;

                /* TODO: we can't support Azure Service Bus plug-ins yet because the new Azure SDK doesn't yet support this:
                         https://github.com/arcus-azure/arcus.messaging/issues/176 */

                RegisterClientInformation(JobId, serviceBusConnectionString.EntityPath);

                return processor;
            }
        }

        private ServiceBusProcessor CreateProcessor(ServiceBusClient client, string entityName, string subscriptionName)
        {
            if (string.IsNullOrWhiteSpace(subscriptionName))
            {
                return client.CreateProcessor(entityName, _messageProcessorOptions);
            }

            return client.CreateProcessor(entityName, subscriptionName, _messageProcessorOptions);
        }

        /// <summary>
        ///     Triggered when the Azure Service Bus message pump is performing a graceful shutdown.
        /// </summary>
        /// <param name="cancellationToken">Indicates that the shutdown process should no longer be graceful.</param>
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            if (Settings.ServiceBusEntity == ServiceBusEntityType.Topic
                && Settings.Options.TopicSubscription.HasValue
                && Settings.Options.TopicSubscription.Value.HasFlag(TopicSubscription.DeleteOnStop))
            {
                await DeleteTopicSubscriptionAsync(cancellationToken);
            }

            await base.StopAsync(cancellationToken);
            _isHostShuttingDown = true;
            _loggingScope.Dispose();
        }

        private async Task DeleteTopicSubscriptionAsync(CancellationToken cancellationToken)
        {
            string rawConnectionString = await GetServiceBusConnectionStringAsync();
            var serviceBusConnectionProperties = ServiceBusConnectionStringProperties.Parse(rawConnectionString);

            var serviceBusClient = new ServiceBusAdministrationClient(rawConnectionString);

            try
            {
                bool subscriptionExists = await serviceBusClient.SubscriptionExistsAsync(serviceBusConnectionProperties.EntityPath, SubscriptionName, cancellationToken);
                if (subscriptionExists)
                {
                    Logger.LogTrace("Deleting subscription '{SubscriptionName}' on topic '{Path}'...", SubscriptionName, serviceBusConnectionProperties.EntityPath);
                    await serviceBusClient.DeleteSubscriptionAsync(serviceBusConnectionProperties.EntityPath, SubscriptionName, cancellationToken);
                    Logger.LogTrace("Subscription '{SubscriptionName}' deleted on topic '{Path}'", SubscriptionName, serviceBusConnectionProperties.EntityPath);
                }
                else
                {
                    Logger.LogTrace("Cannot delete topic subscription with name '{SubscriptionName}' because no subscription exists on Service Bus resource", SubscriptionName);
                }
            }
            catch (Exception exception)
            {
                Logger.LogWarning(exception, "Failed to delete topic subscription with name '{SubscriptionName}' on Service Bus resource", SubscriptionName);
            }
        }

        private async Task<string> GetServiceBusConnectionStringAsync()
        {
            Logger.LogTrace("Getting Azure Service Bus Topic connection string on topic '{TopicPath}'...",  Settings.EntityName);
            string connectionString = await Settings.GetConnectionStringAsync();
            Logger.LogTrace("Got Azure Service Bus Topic connection string on topic '{TopicPath}'",  Settings.EntityName);

            return connectionString;
        }

        private async Task ProcessMessageAsync(ProcessMessageEventArgs args)
        {
            ServiceBusReceivedMessage message = args?.Message;
            if (message is null)
            {
                Logger.LogWarning("Received message on Azure Service Bus message pump '{JobId}' was null, skipping", JobId);
                return;
            }

            if (_isHostShuttingDown)
            {
                Logger.LogWarning("Abandoning message with ID '{MessageId}' as the Azure Service Bus message pump is shutting down",  message.MessageId);
                await args.AbandonMessageAsync(message);
                return;
            }

            if (String.IsNullOrEmpty(message.CorrelationId))
            {
                Logger.LogTrace("No operation ID was found on the message '{MessageId}' during processing in the Azure Service Bus message pump '{JobId}'", message.MessageId, JobId);
            }

            AzureServiceBusMessageContext messageContext = message.GetMessageContext(JobId);
            MessageCorrelationInfo correlationInfo = message.GetCorrelationInfo(Settings.Options.Correlation?.TransactionIdPropertyName ?? PropertyNames.TransactionId);
            ServiceBusReceiver receiver = args.GetServiceBusReceiver();
            
            using (IServiceScope serviceScope = ServiceProvider.CreateScope())
            {
                var correlationInfoAccessor = serviceScope.ServiceProvider.GetService<ICorrelationInfoAccessor<MessageCorrelationInfo>>();
                if (correlationInfoAccessor is null)
                {
                    Logger.LogTrace("No message correlation configured in Azure Service Bus message pump '{JobId}' while processing message '{MessageId}'", JobId, message.MessageId);
                    await _messageRouter.RouteMessageAsync(receiver, args.Message, messageContext, correlationInfo, args.CancellationToken);
                }
                else
                {
                    correlationInfoAccessor.SetCorrelationInfo(correlationInfo);
                    using (LogContext.Push(new MessageCorrelationInfoEnricher(correlationInfoAccessor)))
                    {
                        await _messageRouter.RouteMessageAsync(receiver, args.Message, messageContext, correlationInfo, args.CancellationToken);
                    }
                }
            }
        }

        private static async Task UntilCancelledAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested == false)
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }
        }
    }
}

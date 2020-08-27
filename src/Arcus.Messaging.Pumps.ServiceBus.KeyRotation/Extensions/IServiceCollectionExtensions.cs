﻿using System;
using System.Linq;
using Arcus.BackgroundJobs.CloudEvents;
using Arcus.Messaging.Pumps.ServiceBus.Configuration;
using CloudNative.CloudEvents;
using GuardNet;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Arcus.Messaging.Pumps.ServiceBus.KeyRotation.Extensions
{
    /// <summary>
    /// Extensions on the <see cref="IServiceCollection"/> to add key rotation related functionality.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static class IServiceCollectionExtensions
    {
        /// <summary>
        /// Adds a background job to the <see cref="IServiceCollection"/> to automatically restart a <see cref="AzureServiceBusMessagePump"/> with a specific <paramref name="jobId"/>
        /// when the Azure Key Vault secret that holds the Azure Service Bus connection string was updated.
        /// </summary>
        /// <param name="services">The collection of services to add the job to.</param>
        /// <param name="jobId">The unique background job ID to identify which message pump to restart.</param>
        /// <param name="subscriptionNamePrefix">The name of the Azure Service Bus subscription that will be created to receive <see cref="CloudEvent"/>'s.</param>
        /// <param name="serviceBusTopicConnectionStringSecretKey">The secret key that points to the Azure Service Bus Topic connection string.</param>
        /// <param name="maximumUnauthorizedExceptionsBeforeRestart">
        ///     The fallback when the Azure Key Vault notification doesn't get delivered correctly, how many times should the message pump run into an <see cref="UnauthorizedException"/> before restarting.
        /// </param>
        public static IServiceCollection WithReAuthenticationOnNewSecretVersion(
            this IServiceCollection services,
            string jobId,
            string subscriptionNamePrefix,
            string serviceBusTopicConnectionStringSecretKey,
            int maximumUnauthorizedExceptionsBeforeRestart = 5)
        {
            Guard.NotNull(services, nameof(services));
            Guard.NotNullOrWhitespace(subscriptionNamePrefix, nameof(subscriptionNamePrefix), "Requires a non-blank subscription name of the Azure Service Bus Topic subscription, to receive Azure Key Vault events");
            Guard.NotNullOrWhitespace(serviceBusTopicConnectionStringSecretKey, nameof(serviceBusTopicConnectionStringSecretKey), "Requires a non-blank secret key that points to a Azure Service Bus Topic");
            Guard.NotLessThan(maximumUnauthorizedExceptionsBeforeRestart, 0, nameof(maximumUnauthorizedExceptionsBeforeRestart), "Requires the fallback of maximum unauthorized exception count to be greater than zero");

            services.AddHostedService(serviceProvider =>
            {
                AzureServiceBusMessagePump messagePump =
                    serviceProvider.GetServices<IHostedService>()
                                   .OfType<AzureServiceBusMessagePump>()
                                   .FirstOrDefault(pump => pump.JobId == jobId);

                Guard.NotNull(messagePump, nameof(messagePump), 
                    $"Cannot register re-authentication without a '{nameof(AzureServiceBusMessagePump)}' with 'JobId' = '{jobId}'");

                messagePump.Settings.Options.MaximumUnauthorizedExceptionsBeforeRestart = maximumUnauthorizedExceptionsBeforeRestart;

                var messageHandlerLogger = serviceProvider.GetRequiredService<ILogger<ReAuthenticateMessageHandler>>();
                services.WithServiceBusMessageHandler<ReAuthenticateMessageHandler, CloudEvent>(
                    context => context.JobId == jobId, provider => new ReAuthenticateMessageHandler(messagePump, messageHandlerLogger));


                var settings = new AzureServiceBusMessagePumpSettings(
                    entityName: null,
                    subscriptionName: $"{subscriptionNamePrefix}.{Guid.NewGuid()}",
                    ServiceBusEntity.Topic,
                    getConnectionStringFromConfigurationFunc: null,
                    getConnectionStringFromSecretFunc: secretProvider => secretProvider.GetRawSecretAsync(serviceBusTopicConnectionStringSecretKey),
                    options: new AzureServiceBusMessagePumpConfiguration(AzureServiceBusTopicMessagePumpOptions.Default),
                    serviceProvider: serviceProvider);

                var configuration = serviceProvider.GetRequiredService<IConfiguration>();
                var messagePumpLogger = serviceProvider.GetRequiredService<ILogger<AzureServiceBusMessagePump>>();
                return new CloudEventBackgroundJob(settings, configuration, serviceProvider, messagePumpLogger);
            });

            return services;
        }
    }
}
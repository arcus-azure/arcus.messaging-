﻿using System;
using System.Threading.Tasks;
using Arcus.Messaging.Pumps.ServiceBus;
using Arcus.Messaging.Pumps.ServiceBus.Configuration;
using Arcus.Security.Core;
using GuardNet;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    // ReSharper disable once InconsistentNaming²
    public static class IServiceCollectionExtensions
    {
        /// <summary>
        /// Adds a message handler to consume messages from Azure Service Bus Queue
        /// </summary>
        /// <remarks>
        /// When using this approach; the connection string should be scoped to the queue that is being processed, not the
        /// namespace
        /// </remarks>
        /// <param name="services">Collection of services to use in the application</param>
        /// <param name="getConnectionStringFromSecretFunc">Function to look up the connection string from the secret store</param>
        /// <param name="configureMessagePump">Capability to configure how the message pump should behave</param>
        /// <returns>Collection of services to use in the application</returns>
        public static IServiceCollection AddServiceBusQueueMessagePump<TMessagePump>(
            this IServiceCollection services,
            Func<ISecretProvider, Task<string>> getConnectionStringFromSecretFunc,
            Action<AzureServiceBusQueueMessagePumpOptions> configureMessagePump = null)
            where TMessagePump : class, IHostedService
        {
            Guard.NotNull(services, nameof(services));

            AddServiceBusQueueMessagePump<TMessagePump>(
                services,
                entityName: null,
                getConnectionStringFromSecretFunc: getConnectionStringFromSecretFunc,
                configureQueueMessagePump: configureMessagePump);

            return services;
        }

        /// <summary>
        /// Adds a message handler to consume messages from Azure Service Bus Queue
        /// </summary>
        /// <remarks>
        /// When using this approach; the connection string should be scoped to the queue that is being processed, not the
        /// namespace
        /// </remarks>
        /// <param name="services">Collection of services to use in the application</param>
        /// <param name="getConnectionStringFromConfigurationFunc">Function to look up the connection string from the configuration</param>
        /// <param name="configureMessagePump">Capability to configure how the message pump should behave</param>
        /// <returns>Collection of services to use in the application</returns>
        public static IServiceCollection AddServiceBusQueueMessagePump<TMessagePump>(
            this IServiceCollection services,
            Func<IConfiguration, string> getConnectionStringFromConfigurationFunc,
            Action<AzureServiceBusQueueMessagePumpOptions> configureMessagePump = null)
            where TMessagePump : class, IHostedService
        {
            Guard.NotNull(services, nameof(services));

            AddServiceBusQueueMessagePump<TMessagePump>(
                services,
                entityName: null,
                getConnectionStringFromConfigurationFunc:
                getConnectionStringFromConfigurationFunc,
                configureQueueMessagePump: configureMessagePump);

            return services;
        }

        /// <summary>
        /// Adds a message handler to consume messages from Azure Service Bus Queue
        /// </summary>
        /// <remarks>
        /// When using this approach; the connection string should be scoped to the queue that is being processed, not the
        /// namespace
        /// </remarks>
        /// <param name="services">Collection of services to use in the application</param>
        /// <param name="secretName">
        /// Name of the secret to retrieve using your registered <see cref="ISecretProvider" />
        /// implementation
        /// </param>
        /// <param name="configureMessagePump">Capability to configure how the message pump should behave</param>
        /// <returns>Collection of services to use in the application</returns>
        public static IServiceCollection AddServiceBusQueueMessagePump<TMessagePump>(
            this IServiceCollection services,
            string secretName,
            Action<AzureServiceBusQueueMessagePumpOptions> configureMessagePump = null)
            where TMessagePump : class, IHostedService
        {
            Guard.NotNull(services, nameof(services));

            AddServiceBusQueueMessagePump<TMessagePump>(
                services,
                entityName: null,
                getConnectionStringFromSecretFunc: secretProvider => secretProvider.GetRawSecretAsync(secretName),
                configureQueueMessagePump: configureMessagePump);

            return services;
        }

        /// <summary>
        /// Adds a message handler to consume messages from Azure Service Bus Queue
        /// </summary>
        /// <remarks>
        /// When using this approach; the connection string should be scoped to the queue that is being processed, not the
        /// namespace
        /// </remarks>
        /// <param name="services">Collection of services to use in the application</param>
        /// <param name="queueName">Name of the queue to process</param>
        /// <param name="secretName">
        /// Name of the secret to retrieve using your registered <see cref="ISecretProvider" />
        /// implementation
        /// </param>
        /// <param name="configureMessagePump">Capability to configure how the message pump should behave</param>
        /// <returns>Collection of services to use in the application</returns>
        public static IServiceCollection AddServiceBusQueueMessagePump<TMessagePump>(
            this IServiceCollection services,
            string queueName,
            string secretName,
            Action<AzureServiceBusQueueMessagePumpOptions> configureMessagePump = null)
            where TMessagePump : class, IHostedService
        {
            Guard.NotNull(services, nameof(services));

            AddServiceBusQueueMessagePump<TMessagePump>(
                services,
                entityName: queueName,
                getConnectionStringFromSecretFunc: secretProvider => secretProvider.GetRawSecretAsync(secretName),
                configureQueueMessagePump: configureMessagePump);

            return services;
        }

       

        /// <summary>
        /// Adds a message handler to consume messages from Azure Service Bus Queue
        /// </summary>
        /// <param name="queueName">Name of the queue to process</param>
        /// <param name="services">Collection of services to use in the application</param>
        /// <param name="getConnectionStringFromSecretFunc">Function to look up the connection string from the secret store</param>
        /// <param name="configureMessagePump">Capability to configure how the message pump should behave</param>
        /// <returns>Collection of services to use in the application</returns>
        public static IServiceCollection AddServiceBusQueueMessagePump<TMessagePump>(
            this IServiceCollection services,
            string queueName,
            Func<ISecretProvider, Task<string>> getConnectionStringFromSecretFunc,
            Action<AzureServiceBusQueueMessagePumpOptions> configureMessagePump = null)
            where TMessagePump : class, IHostedService
        {
            Guard.NotNull(services, nameof(services));

            AddServiceBusQueueMessagePump<TMessagePump>(
                services,
                entityName: queueName,
                getConnectionStringFromSecretFunc: getConnectionStringFromSecretFunc,
                configureQueueMessagePump: configureMessagePump);

            return services;
        }

        /// <summary>
        /// Adds a message handler to consume messages from Azure Service Bus Queue
        /// </summary>
        /// <param name="queueName">Name of the queue to process</param>
        /// <param name="services">Collection of services to use in the application</param>
        /// <param name="getConnectionStringFromConfigurationFunc">Function to look up the connection string from the configuration</param>
        /// <param name="configureMessagePump">Capability to configure how the message pump should behave</param>
        /// <returns>Collection of services to use in the application</returns>
        public static IServiceCollection AddServiceBusQueueMessagePump<TMessagePump>(
            this IServiceCollection services,
            string queueName,
            Func<IConfiguration, string> getConnectionStringFromConfigurationFunc,
            Action<AzureServiceBusQueueMessagePumpOptions> configureMessagePump = null)
            where TMessagePump : class, IHostedService
        {
            Guard.NotNull(services, nameof(services));

            AddServiceBusQueueMessagePump<TMessagePump>(
                services,
                entityName: queueName,
                getConnectionStringFromConfigurationFunc,
                configureQueueMessagePump: configureMessagePump);

            return services;
        }

        private static void AddServiceBusQueueMessagePump<TMessagePump>(
            IServiceCollection services,
            string entityName,
            Func<IConfiguration, string> getConnectionStringFromConfigurationFunc = null,
            Func<ISecretProvider, Task<string>> getConnectionStringFromSecretFunc = null,
            Action<AzureServiceBusQueueMessagePumpOptions> configureQueueMessagePump = null)
            where TMessagePump : class, IHostedService
        {
            Guard.NotNull(services, nameof(services));

            AddServiceBusMessagePump<TMessagePump>(
                services,
                entityName,
                subscriptionName: null,
                ServiceBusEntity.Queue,
                configureQueueMessagePump: configureQueueMessagePump,
                getConnectionStringFromConfigurationFunc: getConnectionStringFromConfigurationFunc,
                getConnectionStringFromSecretFunc: getConnectionStringFromSecretFunc);
        }

        /// <summary>
        /// Adds a message handler to consume messages from Azure Service Bus Topic
        /// </summary>
        /// <remarks>
        /// When using this approach; the connection string should be scoped to the topic that is being processed, not the
        /// namespace
        /// </remarks>
        /// <param name="subscriptionName">Name of the subscription to process </param>
        /// <param name="services">Collection of services to use in the application</param>
        /// <param name="secretName">
        /// Name of the secret to retrieve using your registered <see cref="ISecretProvider" />
        /// implementation
        /// </param>
        /// <param name="configureMessagePump">Capability to configure how the message pump should behave</param>
        /// <returns>Collection of services to use in the application</returns>
        public static IServiceCollection AddServiceBusTopicMessagePump<TMessagePump>(
            this IServiceCollection services,
            string subscriptionName,
            string secretName,
            Action<AzureServiceBusTopicMessagePumpOptions> configureMessagePump = null)
            where TMessagePump : class, IHostedService
        {
            Guard.NotNull(services, nameof(services));
            Guard.NotNullOrWhitespace(subscriptionName, nameof(subscriptionName));

            AddServiceBusTopicMessagePump<TMessagePump>(
                services,
                entityName: null,
                subscriptionName: subscriptionName,
                getConnectionStringFromSecretFunc: secretProvider => secretProvider.GetRawSecretAsync(secretName),
                configureTopicMessagePump: configureMessagePump);

            return services;
        }

        /// <summary>
        /// Adds a message handler to consume messages from Azure Service Bus Topic
        /// </summary>
        /// <remarks>
        /// When using this approach; the connection string should be scoped to the topic that is being processed, not the
        /// namespace
        /// </remarks>
        /// <param name="subscriptionName">Name of the subscription to process </param>
        /// <param name="services">Collection of services to use in the application</param>
        /// <param name="getConnectionStringFromSecretFunc">Function to look up the connection string from the secret store</param>
        /// <param name="configureMessagePump">Capability to configure how the message pump should behave</param>
        /// <returns>Collection of services to use in the application</returns>
        public static IServiceCollection AddServiceBusTopicMessagePump<TMessagePump>(
            this IServiceCollection services,
            string subscriptionName,
            Func<ISecretProvider, Task<string>> getConnectionStringFromSecretFunc,
            Action<AzureServiceBusTopicMessagePumpOptions> configureMessagePump = null)
            where TMessagePump : class, IHostedService
        {
            Guard.NotNull(services, nameof(services));
            Guard.NotNullOrWhitespace(subscriptionName, nameof(subscriptionName));

            AddServiceBusTopicMessagePump<TMessagePump>(
                services,
                entityName: null,
                subscriptionName: subscriptionName,
                getConnectionStringFromSecretFunc: getConnectionStringFromSecretFunc,
                configureTopicMessagePump: configureMessagePump);

            return services;
        }

        /// <summary>
        /// Adds a message handler to consume messages from Azure Service Bus Topic
        /// </summary>
        /// <remarks>
        /// When using this approach; the connection string should be scoped to the topic that is being processed, not the
        /// namespace
        /// </remarks>
        /// <param name="subscriptionName">Name of the subscription to process </param>
        /// <param name="services">Collection of services to use in the application</param>
        /// <param name="getConnectionStringFromConfigurationFunc">Function to look up the connection string from the configuration</param>
        /// <param name="configureMessagePump">Capability to configure how the message pump should behave</param>
        /// <returns>Collection of services to use in the application</returns>
        public static IServiceCollection AddServiceBusTopicMessagePump<TMessagePump>(
            this IServiceCollection services,
            string subscriptionName,
            Func<IConfiguration, string> getConnectionStringFromConfigurationFunc,
            Action<AzureServiceBusTopicMessagePumpOptions> configureMessagePump = null)
            where TMessagePump : class, IHostedService
        {
            Guard.NotNull(services, nameof(services));
            Guard.NotNullOrWhitespace(subscriptionName, nameof(subscriptionName));

            AddServiceBusTopicMessagePump<TMessagePump>(
                services,
                entityName: null,
                subscriptionName: subscriptionName,
                getConnectionStringFromConfigurationFunc:
                getConnectionStringFromConfigurationFunc,
                configureTopicMessagePump: configureMessagePump);

            return services;
        }

        /// <summary>
        /// Adds a message handler to consume messages from Azure Service Bus Topic
        /// </summary>
        /// <param name="topicName">Name of the topic to work with</param>
        /// <param name="subscriptionName">
        /// Name of the subscription to process, concat with the
        /// <see cref="AzureServiceBusMessagePumpConfiguration.JobId" />
        /// </param>
        /// <param name="services">Collection of services to use in the application</param>
        /// <param name="secretName">
        /// Name of the secret to retrieve using your registered <see cref="ISecretProvider" />
        /// implementation
        /// </param>
        /// <param name="configureMessagePump">Capability to configure how the message pump should behave</param>
        /// <returns>Collection of services to use in the application</returns>
        public static IServiceCollection AddServiceBusTopicMessagePump<TMessagePump>(
            this IServiceCollection services,
            string topicName,
            string subscriptionName,
            string secretName,
            Action<AzureServiceBusTopicMessagePumpOptions> configureMessagePump = null)
            where TMessagePump : class, IHostedService
        {
            Guard.NotNull(services, nameof(services));
            Guard.NotNullOrWhitespace(subscriptionName, nameof(subscriptionName));

            AddServiceBusTopicMessagePump<TMessagePump>(
                services,
                entityName: topicName,
                subscriptionName,
                getConnectionStringFromSecretFunc: secretProvider => secretProvider.GetRawSecretAsync(secretName),
                configureTopicMessagePump: configureMessagePump);

            return services;
        }

        /// <summary>
        /// Adds a message handler to consume messages from Azure Service Bus Topic
        /// </summary>
        /// <param name="topicName">Name of the topic to work with</param>
        /// <param name="subscriptionName">Name of the subscription to process</param>
        /// <param name="services">Collection of services to use in the application</param>
        /// <param name="getConnectionStringFromSecretFunc">Function to look up the connection string from the secret store</param>
        /// <param name="configureMessagePump">Capability to configure how the message pump should behave</param>
        /// <returns>Collection of services to use in the application</returns>
        public static IServiceCollection AddServiceBusTopicMessagePump<TMessagePump>(
            this IServiceCollection services,
            string topicName,
            string subscriptionName,
            Func<ISecretProvider, Task<string>> getConnectionStringFromSecretFunc,
            Action<AzureServiceBusTopicMessagePumpOptions> configureMessagePump = null)
            where TMessagePump : class, IHostedService
        {
            Guard.NotNull(services, nameof(services));
            Guard.NotNullOrWhitespace(subscriptionName, nameof(subscriptionName));

            AddServiceBusTopicMessagePump<TMessagePump>(
                services,
                entityName: topicName,
                subscriptionName,
                getConnectionStringFromSecretFunc: getConnectionStringFromSecretFunc,
                configureTopicMessagePump: configureMessagePump);

            return services;
        }

        /// <summary>
        /// Adds a message handler to consume messages from Azure Service Bus Topic
        /// </summary>
        /// <param name="topicName">Name of the topic to work with</param>
        /// <param name="subscriptionName">Name of the subscription to process</param>
        /// <param name="services">Collection of services to use in the application</param>
        /// <param name="getConnectionStringFromConfigurationFunc">Function to look up the connection string from the configuration</param>
        /// <param name="configureMessagePump">Capability to configure how the message pump should behave</param>
        /// <returns>Collection of services to use in the application</returns>
        public static IServiceCollection AddServiceBusTopicMessagePump<TMessagePump>(
            this IServiceCollection services,
            string topicName,
            string subscriptionName,
            Func<IConfiguration, string> getConnectionStringFromConfigurationFunc,
            Action<AzureServiceBusTopicMessagePumpOptions> configureMessagePump = null)
            where TMessagePump : class, IHostedService
        {
            Guard.NotNull(services, nameof(services));
            Guard.NotNullOrWhitespace(subscriptionName, nameof(subscriptionName));

            AddServiceBusTopicMessagePump<TMessagePump>(
                services,
                entityName: topicName,
                subscriptionName,
                getConnectionStringFromConfigurationFunc,
                configureTopicMessagePump: configureMessagePump);

            return services;
        }

        /// <summary>
        /// Adds a message handler to consume messages from Azure Service Bus Topic
        /// </summary>
        /// <remarks>
        /// When using this approach; the connection string should be scoped to the topic that is being processed, not the
        /// namespace
        /// </remarks>
        /// <param name="subscriptionPrefix">
        /// Prefix of the subscription to process, concat with the
        /// <see cref="AzureServiceBusMessagePumpConfiguration.JobId" />
        /// </param>
        /// <param name="services">Collection of services to use in the application</param>
        /// <param name="secretName">
        /// Name of the secret to retrieve using your registered <see cref="ISecretProvider" />
        /// implementation
        /// </param>
        /// <param name="configureMessagePump">Capability to configure how the message pump should behave</param>
        /// <returns>Collection of services to use in the application</returns>
        public static IServiceCollection AddServiceBusTopicMessagePumpWithPrefix<TMessagePump>(
            this IServiceCollection services,
            string subscriptionPrefix,
            string secretName,
            Action<AzureServiceBusTopicMessagePumpOptions> configureMessagePump = null)
            where TMessagePump : class, IHostedService
        {
            Guard.NotNull(services, nameof(services));
            Guard.NotNullOrWhitespace(subscriptionPrefix, nameof(subscriptionPrefix));

            AddServiceBusTopicMessagePumpWithPrefix<TMessagePump>(
                services,
                entityName: null,
                subscriptionPrefix,
                getConnectionStringFromSecretFunc: secretProvider => secretProvider.GetRawSecretAsync(secretName),
                configureTopicMessagePump: configureMessagePump);

            return services;
        }

        /// <summary>
        /// Adds a message handler to consume messages from Azure Service Bus Topic
        /// </summary>
        /// <remarks>
        /// When using this approach; the connection string should be scoped to the topic that is being processed, not the
        /// namespace
        /// </remarks>
        /// <param name="subscriptionPrefix">
        /// Prefix of the subscription to process, concat with the
        /// <see cref="AzureServiceBusMessagePumpConfiguration.JobId" />
        /// </param>
        /// <param name="services">Collection of services to use in the application</param>
        /// <param name="getConnectionStringFromSecretFunc">Function to look up the connection string from the secret store</param>
        /// <param name="configureMessagePump">Capability to configure how the message pump should behave</param>
        /// <returns>Collection of services to use in the application</returns>
        public static IServiceCollection AddServiceBusTopicMessagePumpWithPrefix<TMessagePump>(
            this IServiceCollection services,
            string subscriptionPrefix,
            Func<ISecretProvider, Task<string>> getConnectionStringFromSecretFunc,
            Action<AzureServiceBusTopicMessagePumpOptions> configureMessagePump = null)
            where TMessagePump : class, IHostedService
        {
            Guard.NotNull(services, nameof(services));
            Guard.NotNullOrWhitespace(subscriptionPrefix, nameof(subscriptionPrefix));

            AddServiceBusTopicMessagePumpWithPrefix<TMessagePump>(
                services,
                entityName: null,
                subscriptionPrefix,
                getConnectionStringFromSecretFunc: getConnectionStringFromSecretFunc,
                configureTopicMessagePump: configureMessagePump);

            return services;
        }

        /// <summary>
        /// Adds a message handler to consume messages from Azure Service Bus Topic
        /// </summary>
        /// <remarks>
        /// When using this approach; the connection string should be scoped to the topic that is being processed, not the
        /// namespace
        /// </remarks>
        /// <param name="subscriptionPrefix">
        /// Prefix of the subscription to process, concat with the
        /// <see cref="AzureServiceBusMessagePumpConfiguration.JobId" />
        /// </param>
        /// <param name="services">Collection of services to use in the application</param>
        /// <param name="getConnectionStringFromConfigurationFunc">Function to look up the connection string from the configuration</param>
        /// <param name="configureMessagePump">Capability to configure how the message pump should behave</param>
        /// <returns>Collection of services to use in the application</returns>
        public static IServiceCollection AddServiceBusTopicMessagePumpWithPrefix<TMessagePump>(
            this IServiceCollection services,
            string subscriptionPrefix,
            Func<IConfiguration, string> getConnectionStringFromConfigurationFunc,
            Action<AzureServiceBusTopicMessagePumpOptions> configureMessagePump = null)
            where TMessagePump : class, IHostedService
        {
            Guard.NotNull(services, nameof(services));
            Guard.NotNullOrWhitespace(subscriptionPrefix, nameof(subscriptionPrefix));

            AddServiceBusTopicMessagePumpWithPrefix<TMessagePump>(
                services,
                entityName: null,
                subscriptionPrefix: subscriptionPrefix,
                getConnectionStringFromConfigurationFunc:
                getConnectionStringFromConfigurationFunc,
                configureTopicMessagePump: configureMessagePump);

            return services;
        }

        /// <summary>
        /// Adds a message handler to consume messages from Azure Service Bus Topic
        /// </summary>
        /// <param name="topicName">Name of the topic to work with</param>
        /// <param name="subscriptionPrefix">
        /// Prefix of the subscription to process, concat with the
        /// <see cref="AzureServiceBusMessagePumpConfiguration.JobId" />
        /// </param>
        /// <param name="services">Collection of services to use in the application</param>
        /// <param name="secretName">
        /// Name of the secret to retrieve using your registered <see cref="ISecretProvider" />
        /// implementation
        /// </param>
        /// <param name="configureMessagePump">Capability to configure how the message pump should behave</param>
        /// <returns>Collection of services to use in the application</returns>
        public static IServiceCollection AddServiceBusTopicMessagePumpWithPrefix<TMessagePump>(
            this IServiceCollection services,
            string topicName,
            string subscriptionPrefix,
            string secretName,
            Action<AzureServiceBusTopicMessagePumpOptions> configureMessagePump = null)
            where TMessagePump : class, IHostedService
        {
            Guard.NotNull(services, nameof(services));
            Guard.NotNullOrWhitespace(subscriptionPrefix, nameof(subscriptionPrefix));

            AddServiceBusTopicMessagePumpWithPrefix<TMessagePump>(
                services,
                entityName: topicName,
                subscriptionPrefix,
                getConnectionStringFromSecretFunc: secretProvider => secretProvider.GetRawSecretAsync(secretName),
                configureTopicMessagePump: configureMessagePump);

            return services;
        }

        /// <summary>
        /// Adds a message handler to consume messages from Azure Service Bus Topic
        /// </summary>
        /// <param name="topicName">Name of the topic to work with</param>
        /// <param name="subscriptionPrefix">
        /// Prefix of the subscription to process, concat with the
        /// <see cref="AzureServiceBusMessagePumpConfiguration.JobId" />
        /// </param>
        /// <param name="services">Collection of services to use in the application</param>
        /// <param name="getConnectionStringFromSecretFunc">Function to look up the connection string from the secret store</param>
        /// <param name="configureMessagePump">Capability to configure how the message pump should behave</param>
        /// <returns>Collection of services to use in the application</returns>
        public static IServiceCollection AddServiceBusTopicMessagePumpWithPrefix<TMessagePump>(
            this IServiceCollection services,
            string topicName,
            string subscriptionPrefix,
            Func<ISecretProvider, Task<string>> getConnectionStringFromSecretFunc,
            Action<AzureServiceBusTopicMessagePumpOptions> configureMessagePump = null)
            where TMessagePump : class, IHostedService
        {
            Guard.NotNull(services, nameof(services));
            Guard.NotNullOrWhitespace(subscriptionPrefix, nameof(subscriptionPrefix));

            AddServiceBusTopicMessagePumpWithPrefix<TMessagePump>(
                services,
                entityName: topicName,
                subscriptionPrefix,
                getConnectionStringFromSecretFunc: getConnectionStringFromSecretFunc,
                configureTopicMessagePump: configureMessagePump);

            return services;
        }

        /// <summary>
        /// Adds a message handler to consume messages from Azure Service Bus Topic
        /// </summary>
        /// <param name="topicName">Name of the topic to work with</param>
        /// <param name="subscriptionPrefix">
        /// Prefix of the subscription to process, concat with the
        /// <see cref="AzureServiceBusMessagePumpConfiguration.JobId" />
        /// </param>
        /// <param name="services">Collection of services to use in the application</param>
        /// <param name="getConnectionStringFromConfigurationFunc">Function to look up the connection string from the configuration</param>
        /// <param name="configureMessagePump">Capability to configure how the message pump should behave</param>
        /// <returns>Collection of services to use in the application</returns>
        public static IServiceCollection AddServiceBusTopicMessagePumpWithPrefix<TMessagePump>(
            this IServiceCollection services,
            string topicName,
            string subscriptionPrefix,
            Func<IConfiguration, string> getConnectionStringFromConfigurationFunc,
            Action<AzureServiceBusTopicMessagePumpOptions> configureMessagePump = null)
            where TMessagePump : class, IHostedService
        {
            Guard.NotNull(services, nameof(services));
            Guard.NotNullOrWhitespace(subscriptionPrefix, nameof(subscriptionPrefix));

            AddServiceBusTopicMessagePumpWithPrefix<TMessagePump>(
                services,
                entityName: topicName,
                subscriptionPrefix,
                getConnectionStringFromConfigurationFunc,
                configureTopicMessagePump: configureMessagePump);

            return services;
        }

        private static void AddServiceBusTopicMessagePumpWithPrefix<TMessagePump>(
            IServiceCollection services,
            string entityName,
            string subscriptionPrefix,
            Func<IConfiguration, string> getConnectionStringFromConfigurationFunc = null,
            Func<ISecretProvider, Task<string>> getConnectionStringFromSecretFunc = null,
            Action<AzureServiceBusTopicMessagePumpOptions> configureTopicMessagePump = null)
            where TMessagePump : class, IHostedService
        {
            var messagePumpOptions = AzureServiceBusTopicMessagePumpOptions.Default;
            string subscriptionName = $"{subscriptionPrefix}-{messagePumpOptions.JobId}";

            AddServiceBusTopicMessagePump<TMessagePump>(
                services,
                entityName: entityName,
                subscriptionName,
                getConnectionStringFromSecretFunc: getConnectionStringFromSecretFunc,
                getConnectionStringFromConfigurationFunc: getConnectionStringFromConfigurationFunc,
                configureTopicMessagePump: configureTopicMessagePump);
        }

        private static void AddServiceBusTopicMessagePump<TMessagePump>(
            IServiceCollection services,
            string entityName,
            string subscriptionName = null,
            Func<IConfiguration, string> getConnectionStringFromConfigurationFunc = null,
            Func<ISecretProvider, Task<string>> getConnectionStringFromSecretFunc = null,
            Action<AzureServiceBusTopicMessagePumpOptions> configureTopicMessagePump = null)
            where TMessagePump : class, IHostedService
        {
            Guard.NotNull(services, nameof(services));

            AddServiceBusMessagePump<TMessagePump>(
                services,
                entityName,
                subscriptionName,
                ServiceBusEntity.Topic,
                configureTopicMessagePump: configureTopicMessagePump,
                getConnectionStringFromConfigurationFunc: getConnectionStringFromConfigurationFunc,
                getConnectionStringFromSecretFunc: getConnectionStringFromSecretFunc);
        }

        private static void AddServiceBusMessagePump<TMessagePump>(
            IServiceCollection services,
            string entityName,
            string subscriptionName,
            ServiceBusEntity serviceBusEntity,
            Action<AzureServiceBusQueueMessagePumpOptions> configureQueueMessagePump = null,
            Action<AzureServiceBusTopicMessagePumpOptions> configureTopicMessagePump = null,
            Func<IConfiguration, string> getConnectionStringFromConfigurationFunc = null,
            Func<ISecretProvider, Task<string>> getConnectionStringFromSecretFunc = null)
            where TMessagePump : class, IHostedService
        {
            Guard.NotNull(services, nameof(services));

            AzureServiceBusMessagePumpConfiguration options = 
                DetermineAzureServiceBusMessagePumpOptions(configureQueueMessagePump, configureTopicMessagePump);

            services.AddSingleton(serviceProvider =>
            {
                return new AzureServiceBusMessagePumpSettings(
                    entityName,
                    subscriptionName,
                    serviceBusEntity,
                    getConnectionStringFromConfigurationFunc,
                    getConnectionStringFromSecretFunc,
                    options,
                    serviceProvider);
            });
            services.AddHostedService<TMessagePump>();
        }

        private static AzureServiceBusMessagePumpConfiguration DetermineAzureServiceBusMessagePumpOptions(
            Action<AzureServiceBusQueueMessagePumpOptions> configureQueueMessagePump,
            Action<AzureServiceBusTopicMessagePumpOptions> configureTopicMessagePump)
        {
            Guard.For<ArgumentException>(
                () => !(configureQueueMessagePump is null) && !(configureTopicMessagePump is null),
                "Only one of the configurable message pump actions can be set");

            if (!(configureTopicMessagePump is null))
            {
                var topicMessagePumpOptions = AzureServiceBusTopicMessagePumpOptions.Default;
                configureTopicMessagePump.Invoke(topicMessagePumpOptions);
                
                var options = new AzureServiceBusMessagePumpConfiguration(topicMessagePumpOptions);
                return options;
            }
            else if (!(configureQueueMessagePump is null))
            {
                var queueMessagePumpOptions = AzureServiceBusQueueMessagePumpOptions.Default;
                configureQueueMessagePump?.Invoke(queueMessagePumpOptions);

                var options = new AzureServiceBusMessagePumpConfiguration(queueMessagePumpOptions);
                return options;
            }
            else
            {
                return AzureServiceBusMessagePumpConfiguration.Default;
            }
        }
    }
}
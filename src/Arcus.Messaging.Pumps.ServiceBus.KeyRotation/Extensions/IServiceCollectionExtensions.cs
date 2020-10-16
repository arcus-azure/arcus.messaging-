﻿using System;
using System.Linq;
using CloudNative.CloudEvents;
using GuardNet;
using Microsoft.Azure.ServiceBus;
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
        /// <param name="targetConnectionStringKey">The secret key where the connection string credentials are located for the target message pump that needs to be auto-restarted.</param>
        /// <param name="maximumUnauthorizedExceptionsBeforeRestart">
        ///     The fallback when the Azure Key Vault notification doesn't get delivered correctly,
        ///     how many times should the message pump run into an <see cref="UnauthorizedException"/> before restarting.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the <paramref name="services"/> or the searched for <see cref="AzureServiceBusMessagePump"/> based on the given <paramref name="jobId"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="subscriptionNamePrefix"/> or <paramref name="serviceBusTopicConnectionStringSecretKey"/> is blank.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="maximumUnauthorizedExceptionsBeforeRestart"/> is less then zero.</exception>
        public static IServiceCollection WithAutoRestartOnRotatedCredentials(
            this IServiceCollection services,
            string jobId,
            string subscriptionNamePrefix,
            string serviceBusTopicConnectionStringSecretKey,
            string targetConnectionStringKey,
            int maximumUnauthorizedExceptionsBeforeRestart = 5)
        {
            Guard.NotNull(services, nameof(services), "Requires a collection of services to add the re-authentication background job");
            Guard.NotNullOrWhitespace(subscriptionNamePrefix, nameof(subscriptionNamePrefix), "Requires a non-blank subscription name of the Azure Service Bus Topic subscription, to receive Azure Key Vault events");
            Guard.NotNullOrWhitespace(serviceBusTopicConnectionStringSecretKey, nameof(serviceBusTopicConnectionStringSecretKey), "Requires a non-blank secret key that points to a Azure Service Bus Topic");
            Guard.NotNullOrWhitespace(targetConnectionStringKey, nameof(targetConnectionStringKey), "Requires a non-blank secret key that points to the credentials that holds the connection string of the target message pump");
            Guard.NotLessThan(maximumUnauthorizedExceptionsBeforeRestart, 0, nameof(maximumUnauthorizedExceptionsBeforeRestart), "Requires an unauthorized exceptions count that is greater than zero");

            services.AddCloudEventBackgroundJob(subscriptionNamePrefix, serviceBusTopicConnectionStringSecretKey);
            services.WithServiceBusMessageHandler<ReAuthenticateOnRotatedCredentialsMessageHandler, CloudEvent>(serviceProvider =>
            {
                AzureServiceBusMessagePump messagePump =
                    serviceProvider.GetServices<IHostedService>()
                                   .OfType<AzureServiceBusMessagePump>()
                                   .FirstOrDefault(pump => pump.JobId == jobId);

                Guard.NotNull(messagePump, nameof(messagePump), 
                    $"Cannot register re-authentication without a '{nameof(AzureServiceBusMessagePump)}' with 'JobId' = '{jobId}'");

                messagePump.ReconfigureOptions(options => options.MaximumUnauthorizedExceptionsBeforeRestart = maximumUnauthorizedExceptionsBeforeRestart);

                var messageHandlerLogger = serviceProvider.GetRequiredService<ILogger<ReAuthenticateOnRotatedCredentialsMessageHandler>>();
                return new ReAuthenticateOnRotatedCredentialsMessageHandler(targetConnectionStringKey, messagePump, messageHandlerLogger);
            });

            return services;
        }
    }
}
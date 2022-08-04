﻿using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Tests.Core.Generators;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Arcus.Messaging.Tests.Integration.Fixture;
using Arcus.Messaging.Tests.Integration.MessagePump.EventHubs;
using Arcus.Testing.Logging;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Wrap;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Messaging.Tests.Integration.EventHubs
{
    [Collection("Integration")]
    [Trait("Category", "Integration")]
    public class EventHubsSenderExtensionsTests : IAsyncLifetime
    {
        private const string DependencyIdPattern = @"with ID [a-z0-9]{8}\-[a-z0-9]{4}\-[a-z0-9]{4}\-[a-z0-9]{4}\-[a-z0-9]{12}";

        private readonly TestConfig _config;
        private readonly EventHubsConfig _eventHubsConfig;
        private readonly ILogger _logger;

        private TemporaryBlobStorageContainer _blobStorageContainer;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventHubsSenderExtensionsTests" /> class.
        /// </summary>
        public EventHubsSenderExtensionsTests(ITestOutputHelper outputWriter)
        {
            _config = TestConfig.Create();
            _eventHubsConfig = _config.GetEventHubsConfig();
            _logger = new XunitTestLogger(outputWriter);
        }

        [Fact]
        public async Task SendMessage_WithMessageCorrelation_TracksMessage()
        {
            // Arrange
            Order order = OrderGenerator.Generate();
            MessageCorrelationInfo correlation = GenerateMessageCorrelationInfo();
            var logger = new InMemoryLogger();

            await using (var client = new EventHubProducerClient(_eventHubsConfig.EventHubsConnectionString, _eventHubsConfig.EventHubsName))
            {
                await client.SendAsync(new [] { order }, correlation, logger);
            }

            // Assert
            string logMessage = Assert.Single(logger.Messages);
            Assert.Contains("Dependency", logMessage);
            Assert.Matches(DependencyIdPattern, logMessage);

            await RetryAssertUntilServiceBusMessageIsAvailableAsync(message =>
            {
                var actual = message.EventBody.ToObjectFromJson<Order>();
                Assert.Equal(order.Id, actual.Id);
                Assert.Equal(message.Properties[PropertyNames.TransactionId], correlation.TransactionId);
                Assert.False(string.IsNullOrWhiteSpace(message.Properties[PropertyNames.OperationParentId].ToString()));
            });
        }

        [Fact]
        public async Task SendMessage_WithCustomOptions_TracksMessage()
        {
            // Arrange
            Order order = OrderGenerator.Generate();
            MessageCorrelationInfo correlation = GenerateMessageCorrelationInfo();
            var dependencyId = $"parent-{Guid.NewGuid()}";
            string transactionIdPropertyName = "My-Transaction-Id", upstreamServicePropertyName = "My-UpstreamService-Id";
            var logger = new InMemoryLogger();

            await using (var client = new EventHubProducerClient(_eventHubsConfig.EventHubsConnectionString, _eventHubsConfig.EventHubsName))
            {
                await client.SendAsync(new [] { order }, correlation, logger, options =>
                {
                    options.TransactionIdPropertyName = transactionIdPropertyName;
                    options.UpstreamServicePropertyName = upstreamServicePropertyName;
                    options.GenerateDependencyId = () => dependencyId;
                });
            }

            // Assert
            string logMessage = Assert.Single(logger.Messages);
            Assert.Contains("Dependency", logMessage);
            Assert.Matches($"with ID {dependencyId}", logMessage);

            await RetryAssertUntilServiceBusMessageIsAvailableAsync(message =>
            {
                var actual = message.EventBody.ToObjectFromJson<Order>();
                Assert.Equal(order.Id, actual.Id);

                Assert.Equal(correlation.TransactionId, message.Properties[transactionIdPropertyName]);
                Assert.Equal(dependencyId, message.Properties[upstreamServicePropertyName]);
            });
        }

        private static MessageCorrelationInfo GenerateMessageCorrelationInfo()
        {
            return new MessageCorrelationInfo(
                $"operation-{Guid.NewGuid()}",
                $"transaction-{Guid.NewGuid()}",
                $"parent-{Guid.NewGuid()}");
        }

        private async Task RetryAssertUntilServiceBusMessageIsAvailableAsync(Action<EventData> assertion)
        {
            PolicyWrap policy = CreateRetryPolicy();
            EventProcessorClient eventProcessor = CreateEventProcessorClient();

            var isProcessed = false;
            var exceptions = new Collection<Exception>();
            eventProcessor.ProcessErrorAsync += args =>
            {
                exceptions.Add(args.Exception);
                return Task.CompletedTask;
            };
            eventProcessor.ProcessEventAsync += async args =>
            {
                try
                {
                    assertion(args.Data);
                    isProcessed = true;
                    await args.UpdateCheckpointAsync();
                }
                catch (Exception exception)
                {
                    exceptions.Add(exception);
                }
            };
            await eventProcessor.StartProcessingAsync();

            try
            { 
                policy.Execute(() =>
                {
                    if (!isProcessed)
                    {
                        if (exceptions.Count == 1)
                        {
                            throw exceptions[0];
                        }
                
                        throw new AggregateException(exceptions);
                    }
                });
            }
            finally
            {
                await eventProcessor.StopProcessingAsync();
            }
        }

        private EventProcessorClient CreateEventProcessorClient()
        {
            var storageClient = new BlobContainerClient(_eventHubsConfig.StorageConnectionString, _blobStorageContainer.ContainerName);
            var eventProcessor = new EventProcessorClient(storageClient, "$Default", _eventHubsConfig.EventHubsConnectionString, _eventHubsConfig.EventHubsName);
            
            return eventProcessor;
        }

        private static PolicyWrap CreateRetryPolicy()
        {
            PolicyWrap policy =
                Policy.Timeout(TimeSpan.FromSeconds(30))
                      .Wrap(Policy.Handle<Exception>()
                                  .WaitAndRetryForever(index => TimeSpan.FromMilliseconds(500)));
            return policy;
        }

        public async Task InitializeAsync()
        {
           _blobStorageContainer = await TemporaryBlobStorageContainer.CreateAsync(_eventHubsConfig.StorageConnectionString, _logger);
        }

        public async Task DisposeAsync()
        {
            if (_blobStorageContainer != null)
            {
                await _blobStorageContainer.DisposeAsync();
            }
        }
    }
}
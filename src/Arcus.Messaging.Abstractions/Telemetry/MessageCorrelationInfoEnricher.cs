﻿using System;
using Arcus.Messaging.Abstractions.MessageHandling;
using GuardNet;
using Serilog.Core;
using Serilog.Events;

namespace Arcus.Messaging.Abstractions.Telemetry
{
    /// <summary>
    /// Logger enrichment of the <see cref="MessageCorrelationInfo" /> model.
    /// </summary>
    public class MessageCorrelationInfoEnricher : ILogEventEnricher
    {
        private readonly MessageCorrelationInfo _messageCorrelationInfo;
        private readonly MessageCorrelationEnricherOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageCorrelationInfoEnricher" /> class.
        /// </summary>
        /// <param name="messageCorrelationInfo">The current message correlation instance that should be enriched on the log events.</param>
        /// <param name="options">The additional options to change the behavior of the message correlation enrichment on the log events.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="messageCorrelationInfo"/> or <paramref name="options"/> is <c>null</c>.</exception>
        public MessageCorrelationInfoEnricher(MessageCorrelationInfo messageCorrelationInfo, MessageCorrelationEnricherOptions options)
        {
            Guard.NotNull(messageCorrelationInfo, nameof(messageCorrelationInfo), "Requires a message correlation instance to enrich the log events");
            Guard.NotNull(options, nameof(options), "Requires a set of options to control the message correlation enrichment on the log events");

            _messageCorrelationInfo = messageCorrelationInfo;
            _options = options;
        }

        /// <summary>
        /// Enrich the log event.
        /// </summary>
        /// <param name="logEvent">The log event to enrich.</param>
        /// <param name="propertyFactory">Factory for creating new properties to add to the event.</param>
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            AddPropertyIfAbsent(_options.OperationIdPropertyName, _messageCorrelationInfo.OperationId, logEvent, propertyFactory);
            AddPropertyIfAbsent(_options.TransactionIdPropertyName, _messageCorrelationInfo.TransactionId, logEvent, propertyFactory);
            AddPropertyIfAbsent(_options.OperationParentIdPropertyName, _messageCorrelationInfo.OperationParentId, logEvent, propertyFactory);
            AddPropertyIfAbsent(_options.CycleIdPropertyName, _messageCorrelationInfo.CycleId, logEvent, propertyFactory);
        }

        private static void AddPropertyIfAbsent(string propertyName, string propertyValue, LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            if (!string.IsNullOrEmpty(propertyValue))
            {
                LogEventProperty property = propertyFactory.CreateProperty(propertyName, propertyValue);
                logEvent.AddPropertyIfAbsent(property);
            }
        }
    }
}

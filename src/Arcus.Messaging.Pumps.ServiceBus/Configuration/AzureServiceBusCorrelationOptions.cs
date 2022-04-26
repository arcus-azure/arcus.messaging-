﻿using System;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.MessageHandling;
using GuardNet;

namespace Arcus.Messaging.Pumps.ServiceBus.Configuration
{
    /// <summary>
    /// Represents the user-configurable options to control the correlation information tracking
    /// during the receiving of the Azure Service Bus messages in the <see cref="AzureServiceBusMessagePump"/>.
    /// </summary>
    public class AzureServiceBusCorrelationOptions
    {
        private readonly MessageCorrelationOptions _correlationOptions;
        
        private string _transactionIdPropertyName = PropertyNames.TransactionId,
                       _operationParentIdPropertyName = PropertyNames.OperationParentId;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureServiceBusCorrelationOptions" /> class.
        /// </summary>
        public AzureServiceBusCorrelationOptions() : this(new MessageCorrelationOptions())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureServiceBusCorrelationOptions" /> class.
        /// </summary>
        internal AzureServiceBusCorrelationOptions(MessageCorrelationOptions correlationOptions)
        {
            _correlationOptions = correlationOptions;
        }

        /// <summary>
        /// Gets or sets the name of the Azure Service Bus message property to retrieve the transaction ID.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="value"/> is blank.</exception>
        public string TransactionIdPropertyName
        {
            get => _transactionIdPropertyName;
            set
            {
                Guard.NotNullOrWhitespace(value, nameof(value), "Transaction ID message property name for the Azure Service Bus message cannot be blank");
                _transactionIdPropertyName = value;
                _correlationOptions.TransactionIdPropertyName = value;
            }
        }

        /// <summary>
        /// Gets or sets the name of the Azure Service Bus message property to retrieve the operation parent ID.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="value"/> is blank.</exception>
        public string OperationParentIdPropertyName
        {
            get => _operationParentIdPropertyName;
            set
            {
                Guard.NotNullOrWhitespace(value, nameof(value), "Operation parent ID message property name for the Azure Service Bus message cannot be blank");
                _operationParentIdPropertyName = value;
                _correlationOptions.OperationParentIdPropertyName = value;
            }
        }
    }
}

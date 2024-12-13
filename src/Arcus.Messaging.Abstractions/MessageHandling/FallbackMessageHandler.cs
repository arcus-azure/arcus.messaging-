﻿using System;
using System.Threading;
using System.Threading.Tasks;
using GuardNet;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Arcus.Messaging.Abstractions.MessageHandling
{
    /// <summary>
    /// Represents a general message handler used to run a fallback action when the regular message handlers weren't able to handle the message.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message that this fallback message handler can handle.</typeparam>
    /// <typeparam name="TMessageContext">The type of the context in which the message is being processed.</typeparam>
    public class FallbackMessageHandler<TMessage, TMessageContext> 
        where TMessage : class
        where TMessageContext : MessageContext
    {
        private readonly string _jobId;
        private readonly ILogger _logger;

        private FallbackMessageHandler(
            IFallbackMessageHandler<TMessage, TMessageContext> messageHandlerInstance,
            string jobId,
            ILogger logger)
        {
            Guard.NotNull(messageHandlerInstance, nameof(messageHandlerInstance));

            _jobId = jobId;
            _logger = logger ?? NullLogger.Instance;
            MessageHandlerType = messageHandlerInstance.GetType();
            MessageHandlerInstance = messageHandlerInstance;
        }

        /// <summary>
        /// Gets the type of the registered fallback message handler instance.
        /// </summary>
        public Type MessageHandlerType { get; }

        /// <summary>
        /// Gets the object instance of the registered fallback message handler.
        /// </summary>
        public IFallbackMessageHandler<TMessage, TMessageContext> MessageHandlerInstance { get; }

        /// <summary>
        /// Creates a general <see cref="FallbackMessageHandler{TMessage,TMessageContext}"/> via the message handler instance.
        /// </summary>
        /// <param name="messageHandlerInstance">The fallback message handler instance.</param>
        /// <param name="jobId">The job ID to link this fallback message handler to a registered message pump.</param>
        /// <param name="logger">The logger instance to write diagnostic trace messages during the fallback message processing.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="messageHandlerInstance"/> is <c>null</c>.</exception>
        internal static FallbackMessageHandler<TMessage, TMessageContext> Create(
            IFallbackMessageHandler<TMessage, TMessageContext> messageHandlerInstance,
            string jobId,
            ILogger<IFallbackMessageHandler<TMessage, TMessageContext>> logger)
        {
            Guard.NotNull(messageHandlerInstance, nameof(messageHandlerInstance), "Requires the original instance of the fallback message handler");
            return new FallbackMessageHandler<TMessage, TMessageContext>(messageHandlerInstance, jobId, logger);
        }

        /// <summary>
        /// Determines if the given <typeparamref name="TMessageContext"/> matches the generic parameter of this message handler.
        /// </summary>
        /// <param name="messageContext">The context in which the incoming message is processed.</param>
        public bool CanProcessMessageBasedOnContext(TMessageContext messageContext)
        {
            Guard.NotNull(messageContext, nameof(messageContext), "Requires an message context instance to determine if the fallback message handler can process the message");
           
            return _jobId is null || messageContext.JobId == _jobId;
        }

        /// <summary>
        /// Processes a <paramref name="message"/> in a <paramref name="messageContext"/> with a fallback operation.
        /// </summary>
        /// <param name="message">The message that was not be able to be processed by the regular message handlers.</param>
        /// <param name="messageContext">The message context in which the incoming <paramref name="message"/> is processed.</param>
        /// <param name="correlationInfo">The information concerning correlation of telemetry and processes by using a variety of unique identifiers.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the fallback processing.</param>
        /// <returns>
        ///     [true] when the fallback message handler was able to process the <paramref name="message"/> in the <paramref name="messageContext"/>; [false] otherwise.
        /// </returns>
        public async Task<bool> ProcessMessageAsync(
            TMessage message,
            TMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            Guard.NotNull(message, nameof(message), "Requires message content to process the message fallback operation");
            Guard.NotNull(messageContext, nameof(messageContext), "Requires a message context to send to the fallback message handler");
            Guard.NotNull(correlationInfo, nameof(correlationInfo), "Requires correlation information to send to the fallback message handler");

            if (CanProcessMessageBasedOnContext(messageContext))
            {
                try
                {
                    await MessageHandlerInstance.ProcessMessageAsync(message, messageContext, correlationInfo, cancellationToken);
                    return true;
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Fallback message handler '{MessageHandlerType}' failed to handle '{MessageType}' message due to an exception: {Message}", MessageHandlerType.Name, typeof(TMessage).Name, exception.Message);
                    return false;   
                }
            }

            _logger.LogTrace("Fallback message handler '{FallbackMessageHandlerType}' cannot handle message because it was not registered in the correct message context (JobId: {JobId})", MessageHandlerType.Name, _jobId);
            return false;
        }
    }
}

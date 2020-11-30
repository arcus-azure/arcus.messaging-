﻿using System;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Pumps.Abstractions;
using Arcus.Messaging.Pumps.Abstractions.MessageHandling;
using GuardNet;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extensions on the <see cref="IServiceCollection"/> to add an general <see cref="IMessageHandler{TMessage,TMessageContext}"/> implementation.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static partial class IServiceCollectionExtensions
    {
        /// <summary>
        /// Adds a <see cref="IMessageHandler{TMessage}" /> implementation to process the messages from an <see cref="MessagePump"/> implementation.
        /// </summary>
        /// <typeparam name="TMessageHandler">The type of the implementation.</typeparam>
        /// <typeparam name="TMessage">The type of the message that the message handler will process.</typeparam>
        /// <param name="services">The collection of services to use in the application.</param>
        public static IServiceCollection WithMessageHandler<TMessageHandler, TMessage>(this IServiceCollection services)
            where TMessageHandler : class, IMessageHandler<TMessage, MessageContext>
            where TMessage : class
        {
            Guard.NotNull(services, nameof(services));

            services.AddTransient<IMessageHandler<TMessage, MessageContext>, TMessageHandler>();

            return services;
        }

        /// <summary>
        /// Adds a <see cref="IMessageHandler{TMessage}" /> implementation to process the messages from an <see cref="MessagePump"/> implementation.
        /// </summary>
        /// <typeparam name="TMessageHandler">The type of the implementation.</typeparam>
        /// <typeparam name="TMessage">The type of the message that the message handler will process.</typeparam>
        /// <param name="services">The collection of services to use in the application.</param>
        /// <param name="implementationFactory">The function that creates the service.</param>
        public static IServiceCollection WithMessageHandler<TMessageHandler, TMessage>(
            this IServiceCollection services,
            Func<IServiceProvider, TMessageHandler> implementationFactory)
            where TMessageHandler : class, IMessageHandler<TMessage, MessageContext>
            where TMessage : class
        {
            Guard.NotNull(services, nameof(services));
            Guard.NotNull(implementationFactory, nameof(implementationFactory));

            services.AddTransient<IMessageHandler<TMessage, MessageContext>, TMessageHandler>(implementationFactory);

            return services;
        }

        /// <summary>
        /// Adds a <see cref="IMessageHandler{TMessage, TMessageContext}" /> implementation to process the messages from an <see cref="MessagePump"/> implementation.
        /// </summary>
        /// <typeparam name="TMessageHandler">The type of the implementation.</typeparam>
        /// <typeparam name="TMessage">The type of the message that the message handler will process.</typeparam>
        /// <typeparam name="TMessageContext">The type of the context in which the message handler will process the message.</typeparam>
        /// <param name="services">The collection of services to use in the application.</param>
        public static IServiceCollection WithMessageHandler<TMessageHandler, TMessage, TMessageContext>(this IServiceCollection services)
            where TMessageHandler : class, IMessageHandler<TMessage, TMessageContext> 
            where TMessage : class
            where TMessageContext : MessageContext
        {
            Guard.NotNull(services, nameof(services));

            services.AddTransient<IMessageHandler<TMessage, TMessageContext>, TMessageHandler>();

            return services;
        }

        /// <summary>
        /// Adds a <see cref="IMessageHandler{TMessage, TMessageContext}" /> implementation to process the messages from an <see cref="MessagePump"/> implementation.
        /// resources.
        /// </summary>
        /// <typeparam name="TMessageHandler">The type of the implementation.</typeparam>
        /// <typeparam name="TMessage">The type of the message that the message handler will process.</typeparam>
        /// <typeparam name="TMessageContext">The type of the context in which the message handler will process the message.</typeparam>
        /// <param name="services">The collection of services to use in the application.</param>
        /// <param name="implementationFactory">The function that creates the message handler.</param>
        public static IServiceCollection WithMessageHandler<TMessageHandler, TMessage, TMessageContext>(
            this IServiceCollection services,
            Func<IServiceProvider, TMessageHandler> implementationFactory)
            where TMessageHandler : class, IMessageHandler<TMessage, TMessageContext> 
            where TMessage : class
            where TMessageContext : MessageContext
        {
            Guard.NotNull(services, nameof(services));
            Guard.NotNull(implementationFactory, nameof(implementationFactory));

            services.AddTransient<IMessageHandler<TMessage, TMessageContext>, TMessageHandler>(implementationFactory);

            return services;
        }

        /// <summary>
        /// Adds a <see cref="IMessageHandler{TMessage}" /> implementation to process the messages from an <see cref="MessagePump"/> implementation.
        /// resources.
        /// </summary>
        /// <typeparam name="TMessageHandler">The type of the implementation.</typeparam>
        /// <typeparam name="TMessage">The type of the message that the message handler will process.</typeparam>
        /// <param name="services">The collection of services to use in the application.</param>
        /// <param name="messageContextFilter">The function that determines if the message handler should handle the message based on the context.</param>
        public static IServiceCollection WithMessageHandler<TMessageHandler, TMessage>(
            this IServiceCollection services,
            Func<MessageContext, bool> messageContextFilter)
            where TMessageHandler : class, IMessageHandler<TMessage, MessageContext>
            where TMessage : class
        {
            Guard.NotNull(services, nameof(services));
            Guard.NotNull(messageContextFilter, nameof(messageContextFilter));

            return services.WithMessageHandler<TMessageHandler, TMessage, MessageContext>(
                messageContextFilter, serviceProvider => ActivatorUtilities.CreateInstance<TMessageHandler>(serviceProvider));
        }

        /// <summary>
        /// Adds a <see cref="IMessageHandler{TMessage, TMessageContext}" /> implementation to process the messages from an <see cref="MessagePump"/> implementation.
        /// resources.
        /// </summary>
        /// <typeparam name="TMessageHandler">The type of the implementation.</typeparam>
        /// <typeparam name="TMessage">The type of the message that the message handler will process.</typeparam>
        /// <typeparam name="TMessageContext">The type of the context in which the message handler will process the message.</typeparam>
        /// <param name="services">The collection of services to use in the application.</param>
        /// <param name="messageContextFilter">The function that determines if the message handler should handle the message based on the context.</param>
        public static IServiceCollection WithMessageHandler<TMessageHandler, TMessage, TMessageContext>(
            this IServiceCollection services,
            Func<TMessageContext, bool> messageContextFilter)
            where TMessageHandler : class, IMessageHandler<TMessage, TMessageContext>
            where TMessage : class
            where TMessageContext : MessageContext
        {
            Guard.NotNull(services, nameof(services));
            Guard.NotNull(messageContextFilter, nameof(messageContextFilter));

            return services.WithMessageHandler<TMessageHandler, TMessage, TMessageContext>(
                messageContextFilter, serviceProvider => ActivatorUtilities.CreateInstance<TMessageHandler>(serviceProvider));
        }

        /// <summary>
        /// Adds a <see cref="IMessageHandler{TMessage}" /> implementation to process the messages from an <see cref="MessagePump"/> implementation.
        /// resources.
        /// </summary>
        /// <typeparam name="TMessageHandler">The type of the implementation.</typeparam>
        /// <typeparam name="TMessage">The type of the message that the message handler will process.</typeparam>
        /// <param name="services">The collection of services to use in the application.</param>
        /// <param name="messageContextFilter">The function that determines if the message handler should handle the message based on the context.</param>
        /// <param name="implementationFactory">The function that creates the service.</param>
        public static IServiceCollection WithMessageHandler<TMessageHandler, TMessage>(
            this IServiceCollection services,
            Func<MessageContext, bool> messageContextFilter,
            Func<IServiceProvider, TMessageHandler> implementationFactory)
            where TMessageHandler : class, IMessageHandler<TMessage, MessageContext>
            where TMessage : class
        {
            Guard.NotNull(services, nameof(services));
            Guard.NotNull(messageContextFilter, nameof(messageContextFilter));
            Guard.NotNull(implementationFactory, nameof(implementationFactory));

            return services.WithMessageHandler<TMessageHandler, TMessage, MessageContext>(messageContextFilter, implementationFactory);
        }

        /// <summary>
        /// Adds a <see cref="IMessageHandler{TMessage, TMessageContext}" /> implementation to process the messages from an <see cref="MessagePump"/> implementation.
        /// resources.
        /// </summary>
        /// <typeparam name="TMessageHandler">The type of the implementation.</typeparam>
        /// <typeparam name="TMessage">The type of the message that the message handler will process.</typeparam>
        /// <typeparam name="TMessageContext">The type of the context in which the message handler will process the message.</typeparam>
        /// <param name="services">The collection of services to use in the application.</param>
        /// <param name="messageContextFilter">The function that determines if the message handler should handle the message based on the context.</param>
        /// <param name="implementationFactory">The function that creates the message handler.</param>
        public static IServiceCollection WithMessageHandler<TMessageHandler, TMessage, TMessageContext>(
            this IServiceCollection services,
            Func<TMessageContext, bool> messageContextFilter,
            Func<IServiceProvider, TMessageHandler> implementationFactory)
            where TMessageHandler : class, IMessageHandler<TMessage, TMessageContext>
            where TMessage : class
            where TMessageContext : MessageContext
        {
            Guard.NotNull(services, nameof(services));
            Guard.NotNull(messageContextFilter,  nameof(messageContextFilter));
            Guard.NotNull(implementationFactory, nameof(implementationFactory));

            return services.AddTransient<IMessageHandler<TMessage, TMessageContext>, MessageHandlerRegistration<TMessage, TMessageContext>>(
                serviceProvider => new MessageHandlerRegistration<TMessage, TMessageContext>(
                    messageContextFilter, implementationFactory(serviceProvider)));
        }

        /// <summary>
        /// Adds a <see cref="IMessageHandler{TMessage, TMessageContext}" /> implementation to process the messages from an <see cref="MessagePump"/> implementation.
        /// resources.
        /// </summary>
        /// <typeparam name="TMessageHandler">The type of the implementation.</typeparam>
        /// <typeparam name="TMessage">The type of the message that the message handler will process.</typeparam>
        /// <typeparam name="TMessageContext">The type of the context in which the message handler will process the message.</typeparam>
        /// <param name="services">The collection of services to use in the application.</param>
        /// <param name="messageHandlerImplementationFactory">The function that creates the message handler.</param>
        /// <param name="messageBodySerializerImplementationFactory">The <see cref="IMessageBodySerializer"/> that custom deserializes the incoming message for the <see cref="IMessageHandler{TMessage,TMessageContext}"/>.</param>
        public static IServiceCollection WithMessageHandler<TMessageHandler, TMessage, TMessageContext>(
            this IServiceCollection services,
            Func<IServiceProvider, IMessageBodySerializer> messageBodySerializerImplementationFactory,
            Func<IServiceProvider, TMessageHandler> messageHandlerImplementationFactory)
            where TMessageHandler : class, IMessageHandler<TMessage, TMessageContext>
            where TMessage : class
            where TMessageContext : MessageContext
        {
            Guard.NotNull(services, nameof(services));
            Guard.NotNull(messageHandlerImplementationFactory, nameof(messageHandlerImplementationFactory));
            Guard.NotNull(messageBodySerializerImplementationFactory, nameof(messageBodySerializerImplementationFactory), "Requires an custom message body serializer instance to deserialize incoming message for the message handler");

            return services.AddTransient<IMessageHandler<TMessage, TMessageContext>, MessageHandlerRegistration<TMessage, TMessageContext>>(
                serviceProvider => new MessageHandlerRegistration<TMessage, TMessageContext>(
                    messageBodySerializerImplementationFactory(serviceProvider),
                    messageHandlerImplementationFactory(serviceProvider)));
        }

        /// <summary>
        /// Adds an <see cref="IFallbackMessageHandler"/> implementation which the message pump can use to fall back to when no message handler is found to process the message.
        /// </summary>
        /// <typeparam name="TMessageHandler">The type of the fallback message handler.</typeparam>
        /// <param name="services">The services to add the fallback message handler to.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/> is <c>null</c>.</exception>
        public static IServiceCollection WithFallbackMessageHandler<TMessageHandler>(this IServiceCollection services)
            where TMessageHandler : class, IFallbackMessageHandler
        {
            Guard.NotNull(services, nameof(services), "Requires a services collection to add the fallback message handler to");

            return services.AddSingleton<IFallbackMessageHandler, TMessageHandler>();
        }

        /// <summary>
        /// Adds an <see cref="IFallbackMessageHandler"/> implementation which the message pump can use to fall back to when no message handler is found to process the message.
        /// </summary>
        /// <typeparam name="TMessageHandler">The type of the fallback message handler.</typeparam>
        /// <param name="services">The services to add the fallback message handler to.</param>
        /// <param name="createImplementation">The function to create the fallback message handler.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/> or the <paramref name="createImplementation"/> is <c>null</c>.</exception>
        public static IServiceCollection WithFallbackMessageHandler<TMessageHandler>(
            this IServiceCollection services,
            Func<IServiceProvider, TMessageHandler> createImplementation)
            where TMessageHandler : class, IFallbackMessageHandler
        {
            Guard.NotNull(services, nameof(services), "Requires a services collection to add the fallback message handler to");
            Guard.NotNull(createImplementation, nameof(createImplementation), "Requires a function to create the fallback message handler");

            return services.AddSingleton<IFallbackMessageHandler, TMessageHandler>(createImplementation);
        }

        /// <summary>
        /// Adds an <see cref="IMessageBodySerializer"/> implementation which the message pump can use to
        /// deserialize the incoming message before processing through the registered <see cref="IMessageHandler{TMessage,TMessageContext}"/> instances.
        /// </summary>
        /// <typeparam name="TMessageBodyHandler">the type of the message body handler.</typeparam>
        /// <param name="services">The services to add the message body handler to.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/> is <c>null</c>.</exception>
        public static IServiceCollection WithMessageBodySerializer<TMessageBodyHandler>(this IServiceCollection services) 
            where TMessageBodyHandler : class, IMessageBodySerializer
        {
            Guard.NotNull(services, nameof(services), "Requires a services collection to add the message body handler to");

            return services.AddSingleton<IMessageBodySerializer, TMessageBodyHandler>();
        }

        /// <summary>
        /// Adds an <see cref="IMessageBodySerializer"/> implementation which the message pump can use to
        /// deserialize the incoming message before processing through the registered <see cref="IMessageHandler{TMessage,TMessageContext}"/> instances.
        /// </summary>
        /// <typeparam name="TMessageBodyHandler">the type of the message body handler.</typeparam>
        /// <param name="services">The services to add the message body handler to.</param>
        /// <param name="createImplementation">The function to create the message body handler.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/> or the <paramref name="createImplementation"/> is <c>null</c>.</exception>
        public static IServiceCollection WithMessageBodySerializer<TMessageBodyHandler>(
            this IServiceCollection services,
            Func<IServiceProvider, TMessageBodyHandler> createImplementation)
            where TMessageBodyHandler : class, IMessageBodySerializer
        {
            Guard.NotNull(services, nameof(services), "Requires a services collection to add the message body handler to");
            Guard.NotNull(createImplementation, nameof(createImplementation), "Requires a function to create the message body handler");

            return services.AddSingleton<IMessageBodySerializer, TMessageBodyHandler>(createImplementation);
        }
    }
}

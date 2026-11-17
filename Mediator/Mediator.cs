using Microsoft.Extensions.DependencyInjection;

namespace Mediator;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Wrappers;

/// <summary>
/// Default mediator implementation relying on single instance delegates for resolving handlers.
/// </summary>
public class Mediator : IMediator
{
    private readonly IServiceProvider _serviceProvider;
    private static readonly ConcurrentDictionary<Type, RequestHandlerBase> _requestHandlers = new();
    private static readonly ConcurrentDictionary<Type, RequestHandlerBase> _queryHandlers = new();
    private static readonly ConcurrentDictionary<Type, StreamRequestHandlerBase> _streamRequestHandlers = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="Mediator"/> class.
    /// </summary>
    /// <param name="serviceProvider">Service provider. Can be a scoped or root provider</param>
    public Mediator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Task<TResponse> Send<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken = default)
    {
        if (command == null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        var handler = (RequestHandlerWrapper<TResponse>)_requestHandlers.GetOrAdd(command.GetType(), static commandType =>
        {
            var wrapperType = typeof(RequestHandlerWrapperImpl<,>).MakeGenericType(commandType, typeof(TResponse));
            var wrapper = Activator.CreateInstance(wrapperType) ?? throw new InvalidOperationException($"Could not create wrapper type for {commandType}");
            return (RequestHandlerBase)wrapper;
        });

        return handler.Handle(command, _serviceProvider, cancellationToken);
    }

    public Task Send<TCommand>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : ICommand
    {
        if (command == null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        var handler = (RequestHandlerWrapper)_requestHandlers.GetOrAdd(command.GetType(), static commandType =>
        {
            var wrapperType = typeof(RequestHandlerWrapperImpl<>).MakeGenericType(commandType);
            var wrapper = Activator.CreateInstance(wrapperType) ?? throw new InvalidOperationException($"Could not create wrapper type for {commandType}");
            return (RequestHandlerBase)wrapper;
        });

        return handler.Handle(command, _serviceProvider, cancellationToken);
    }

    public Task<TResponse> Send<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken = default)
    {
        if (query == null)
        {
            throw new ArgumentNullException(nameof(query));
        }

        var handler = (QueryRequestHandlerWrapper<TResponse>)_queryHandlers.GetOrAdd(query.GetType(), static queryType =>
        {
            var wrapperType = typeof(QueryRequestHandlerWrapperImpl<,>).MakeGenericType(queryType, typeof(TResponse));
            var wrapper = Activator.CreateInstance(wrapperType) ?? throw new InvalidOperationException($"Could not create wrapper type for {queryType}");
            return (RequestHandlerBase)wrapper;
        });

        return handler.Handle(query, _serviceProvider, cancellationToken);
    }

    public Task<object?> Send(object request, CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var requestType = request.GetType();
        var interfaces = requestType.GetInterfaces();

        var commandInterfaceType = interfaces.FirstOrDefault(static i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommand<>));
        if (commandInterfaceType is not null)
        {
            var responseType = commandInterfaceType.GetGenericArguments()[0];
            var wrapperType = typeof(RequestHandlerWrapperImpl<,>).MakeGenericType(requestType, responseType);
            var handler = (RequestHandlerBase)_requestHandlers.GetOrAdd(requestType, static (t, wt) =>
                (RequestHandlerBase)(Activator.CreateInstance(wt) ?? throw new InvalidOperationException($"Could not create wrapper for type {t}")), wrapperType);
            return handler.Handle(request, _serviceProvider, cancellationToken);
        }

        var queryInterfaceType = interfaces.FirstOrDefault(static i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQuery<>));
        if (queryInterfaceType is not null)
        {
            var responseType = queryInterfaceType.GetGenericArguments()[0];
            var wrapperType = typeof(QueryRequestHandlerWrapperImpl<,>).MakeGenericType(requestType, responseType);
            var handler = (RequestHandlerBase)_queryHandlers.GetOrAdd(requestType, static (t, wt) =>
                (RequestHandlerBase)(Activator.CreateInstance(wt) ?? throw new InvalidOperationException($"Could not create wrapper for type {t}")), wrapperType);
            return handler.Handle(request, _serviceProvider, cancellationToken);
        }

        if (interfaces.Any(static i => i == typeof(ICommand)))
        {
            var wrapperType = typeof(RequestHandlerWrapperImpl<>).MakeGenericType(requestType);
            var handler = (RequestHandlerBase)_requestHandlers.GetOrAdd(requestType, static (t, wt) =>
                (RequestHandlerBase)(Activator.CreateInstance(wt) ?? throw new InvalidOperationException($"Could not create wrapper for type {t}")), wrapperType);
            return handler.Handle(request, _serviceProvider, cancellationToken);
        }

        throw new ArgumentException($"{requestType.Name} does not implement {nameof(ICommand)} or {nameof(IQuery<object>)}, cannot send", nameof(request));
    }

    public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var streamHandler = (StreamRequestHandlerWrapper<TResponse>)_streamRequestHandlers.GetOrAdd(request.GetType(), static requestType =>
        {
            var wrapperType = typeof(StreamRequestHandlerWrapperImpl<,>).MakeGenericType(requestType, typeof(TResponse));
            var wrapper = Activator.CreateInstance(wrapperType) ?? throw new InvalidOperationException($"Could not create wrapper for type {requestType}");
            return (StreamRequestHandlerBase)wrapper;
        });

        var items = streamHandler.Handle(request, _serviceProvider, cancellationToken);

        return items;
    }

    public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var handler = _streamRequestHandlers.GetOrAdd(request.GetType(), static requestType =>
        {
            var requestInterfaceType = requestType.GetInterfaces().FirstOrDefault(static i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IStreamRequest<>));
            if (requestInterfaceType is null)
            {
                throw new ArgumentException($"{requestType.Name} does not implement IStreamRequest<TResponse>", nameof(request));
            }

            var responseType = requestInterfaceType.GetGenericArguments()[0];
            var wrapperType = typeof(StreamRequestHandlerWrapperImpl<,>).MakeGenericType(requestType, responseType);
            var wrapper = Activator.CreateInstance(wrapperType) ?? throw new InvalidOperationException($"Could not create wrapper for type {requestType}");
            return (StreamRequestHandlerBase)wrapper;
        });

        var items = handler.Handle(request, _serviceProvider, cancellationToken);

        return items;
    }
}

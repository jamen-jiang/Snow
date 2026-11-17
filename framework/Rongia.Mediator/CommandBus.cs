using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Rongia.Mediator.Wrappers;

namespace Rongia.Mediator;

/// <summary>
/// Default <see cref="ICommandBus"/> implementation relying on cached wrappers for resolving command handlers.
/// </summary>
public class CommandBus : ICommandBus
{
    private static readonly ConcurrentDictionary<Type, RequestHandlerBase> _requestHandlers = new();

    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandBus"/> class.
    /// </summary>
    /// <param name="serviceProvider">Service provider. Can be a scoped or root provider</param>
    public CommandBus(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
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
}

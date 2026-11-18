using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Rongia.DependencyInjection;
using Rongia.Messaging.Wrappers;

namespace Rongia.Messaging;

/// <summary>
/// <see cref="ICommandBus"/> 的默认实现:按命令类型缓存闭式包装器,避免每次发送都做反射
/// </summary>
[ExposeServices(typeof(ICommandBus))]
public class CommandBus : ICommandBus, ITransientDependency
{
    private static readonly ConcurrentDictionary<Type, RequestHandlerBase> _requestHandlers = new();

    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="serviceProvider">服务提供者,可以是作用域内或根容器</param>
    public CommandBus(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public Task<TResponse> SendAsync<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken = default)
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
    public Task SendAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
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

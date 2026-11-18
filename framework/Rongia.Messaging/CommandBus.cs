using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Rongia.DependencyInjection;
using Rongia.Messaging.Wrappers;

namespace Rongia.Messaging;

/// <summary>
/// <see cref="ICommandBus"/> 的默认实现:按命令类型缓存包装器的调用委托,避免每次发送都做反射
/// </summary>
[ExposeServices(typeof(ICommandBus))]
public class CommandBus : ICommandBus, ITransientDependency
{
    private static readonly ConcurrentDictionary<Type, object> _requestHandlers = new();

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

        var handler = (Func<ICommand<TResponse>, IServiceProvider, CancellationToken, Task<TResponse>>)_requestHandlers.GetOrAdd(
            command.GetType(), static commandType =>
            {
                var wrapperType = typeof(CommandHandlerWrapper<,>).MakeGenericType(commandType, typeof(TResponse));
                object wrapper = Activator.CreateInstance(wrapperType) ?? throw new InvalidOperationException($"Could not create wrapper type for {commandType}");
                return wrapperType.GetMethod(nameof(CommandHandlerWrapper<ICommand<TResponse>, TResponse>.Handle))!
                    .CreateDelegate(typeof(Func<ICommand<TResponse>, IServiceProvider, CancellationToken, Task<TResponse>>), wrapper);
            });

        return handler(command, _serviceProvider, cancellationToken);
    }

    /// <inheritdoc />
    public Task SendAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : ICommand
    {
        if (command == null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        var handler = (Func<ICommand, IServiceProvider, CancellationToken, Task<Unit>>)_requestHandlers.GetOrAdd(
            command.GetType(), static commandType =>
            {
                var wrapperType = typeof(CommandHandlerWrapper<>).MakeGenericType(commandType);
                object wrapper = Activator.CreateInstance(wrapperType) ?? throw new InvalidOperationException($"Could not create wrapper type for {commandType}");
                return wrapperType.GetMethod(nameof(CommandHandlerWrapper<ICommand>.Handle))!
                    .CreateDelegate(typeof(Func<ICommand, IServiceProvider, CancellationToken, Task<Unit>>), wrapper);
            });

        return handler(command, _serviceProvider, cancellationToken);
    }
}

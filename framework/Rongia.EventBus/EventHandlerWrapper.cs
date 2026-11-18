using Microsoft.Extensions.DependencyInjection;

namespace Rongia.EventBus;

/// <summary>
/// 处理器包装器: 反射层与强类型调用层之间的桥, 每个事件类型一个实例
/// </summary>
public abstract class EventHandlerWrapper
{
    /// <summary>
    /// 执行该事件的全部处理器
    /// </summary>
    /// <param name="event">事件实例</param>
    /// <param name="serviceProvider">服务提供器</param>
    /// <param name="publishAsync">发布策略</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步任务</returns>
    public abstract Task HandleAsync(IEvent @event, IServiceProvider serviceProvider,
        Func<IEnumerable<EventHandlerExecutor>, IEvent, CancellationToken, Task> publishAsync,
        CancellationToken cancellationToken);
}

/// <summary>
/// 指定事件类型的处理器包装器实现: 为每个处理器实现类型创建独立作用域后再执行
/// </summary>
/// <typeparam name="TEvent">事件类型</typeparam>
public sealed class EventHandlerWrapperImpl<TEvent> : EventHandlerWrapper where TEvent : IEvent
{
    private readonly Type[] _handlerTypes;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="handlerTypes">该事件的全部处理器实现类型</param>
    public EventHandlerWrapperImpl(Type[] handlerTypes)
    {
        _handlerTypes = handlerTypes ?? Type.EmptyTypes;
    }

    /// <inheritdoc />
    public override Task HandleAsync(IEvent @event, IServiceProvider serviceProvider,
        Func<IEnumerable<EventHandlerExecutor>, IEvent, CancellationToken, Task> publishAsync,
        CancellationToken cancellationToken)
    {
        if (_handlerTypes.Length == 0)
        {
            return Task.CompletedTask;
        }

        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

        IEnumerable<EventHandlerExecutor> executors = _handlerTypes.Select(handlerType => CreateExecutor(scopeFactory, handlerType));

        return publishAsync(executors, @event, cancellationToken);
    }

    /// <summary>
    /// 为单个处理器创建独立作用域与执行单元
    /// </summary>
    /// <param name="scopeFactory">作用域工厂</param>
    /// <param name="handlerType">处理器实现类型</param>
    /// <returns>执行单元</returns>
    private static EventHandlerExecutor CreateExecutor(IServiceScopeFactory scopeFactory, Type handlerType)
    {
        var scope = scopeFactory.CreateScope();

        IEventHandler<TEvent> handler;

        try
        {
            handler = (IEventHandler<TEvent>)scope.ServiceProvider.GetRequiredService(handlerType);
        }
        catch
        {
            scope.Dispose();
            throw;
        }

        return new EventHandlerExecutor(handler, async (theEvent, theToken) =>
        {
            using (scope)
            {
                await handler.HandleAsync((TEvent)theEvent, theToken).ConfigureAwait(false);
            }
        });
    }
}

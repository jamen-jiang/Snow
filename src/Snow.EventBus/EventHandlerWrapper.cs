using Microsoft.Extensions.DependencyInjection;

namespace Snow.EventBus;

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
/// 指定事件类型的处理器包装器实现: 每次发布创建一个 DI 作用域, 在该作用域内解析并执行全部
/// <c>IEventHandler&lt;TEvent&gt;</c> 实现
/// </summary>
/// <typeparam name="TEvent">事件类型</typeparam>
public sealed class EventHandlerWrapperImpl<TEvent> : EventHandlerWrapper where TEvent : IEvent
{
    /// <inheritdoc />
    /// <param name="event">事件对象</param>
    /// <param name="serviceProvider">服务提供器</param>
    /// <param name="publishAsync">事件发布委托</param>
    /// <param name="cancellationToken">取消令牌</param>
    public override async Task HandleAsync(IEvent @event, IServiceProvider serviceProvider,
        Func<IEnumerable<EventHandlerExecutor>, IEvent, CancellationToken, Task> publishAsync,
        CancellationToken cancellationToken)
    {
        IServiceScopeFactory scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

        // 处理器可能是 Scoped: 解析和调用必须落在同一个作用域里,
        // 否则从根容器解析会变成 captive 实例(开启作用域校验时直接抛异常)。
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();

        List<EventHandlerExecutor> executors = new();

        foreach (IEventHandler<TEvent> handler in scope.ServiceProvider.GetServices<IEventHandler<TEvent>>())
        {
            executors.Add(new EventHandlerExecutor(
                handler,
                (theEvent, theToken) => handler.HandleAsync((TEvent)theEvent, theToken)));
        }

        if (executors.Count == 0)
        {
            return;
        }

        await publishAsync(executors, @event, cancellationToken);
    }
}

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
/// 指定事件类型的处理器包装器实现: 从容器取 <c>IEventHandler&lt;TEvent&gt;</c> 的全部实现,
/// 为每个处理器创建独立作用域后再执行
/// </summary>
/// <typeparam name="TEvent">事件类型</typeparam>
public sealed class EventHandlerWrapperImpl<TEvent> : EventHandlerWrapper where TEvent : IEvent
{
    /// <inheritdoc />
    /// <param name="event">事件对象</param>
    /// <param name="serviceProvider">服务提供者</param>
    /// <param name="publishAsync">事件发布委托</param>
    /// <param name="cancellationToken">取消令牌</param>
    public override Task HandleAsync(IEvent @event, IServiceProvider serviceProvider,
        Func<IEnumerable<EventHandlerExecutor>, IEvent, CancellationToken, Task> publishAsync,
        CancellationToken cancellationToken)
    {
        IServiceScopeFactory scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

        List<EventHandlerExecutor> executors = new();

        // 作用域必须在解析时就建立: 处理器可能是 Scoped, 解析出来就要保证作用域生命周期覆盖调用
        foreach (IEventHandler<TEvent> handler in serviceProvider.GetServices<IEventHandler<TEvent>>())
        {
            executors.Add(new EventHandlerExecutor(handler, async (theEvent, theToken) =>
            {
                await handler.HandleAsync((TEvent)theEvent, theToken).ConfigureAwait(false);
            }));
        }

        if (executors.Count == 0)
        {
            return Task.CompletedTask;
        }

        return publishAsync(executors, @event, cancellationToken);
    }
}

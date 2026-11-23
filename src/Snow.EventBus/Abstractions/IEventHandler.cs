namespace Snow.EventBus;

/// <summary>
/// 事件处理器标记接口: 非泛型基接口, 供运行时按实现类型识别处理器而无需知道事件类型
/// </summary>
public interface IEventHandler
{
}

/// <summary>
/// 事件处理器: 一个处理器可以同时实现多个 <see cref="IEventHandler{TEvent}"/>
/// </summary>
/// <typeparam name="TEvent">事件类型</typeparam>
public interface IEventHandler<in TEvent> : IEventHandler where TEvent : IEvent
{
    /// <summary>
    /// 处理事件
    /// </summary>
    /// <param name="event">事件实例</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步任务</returns>
    Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default);
}

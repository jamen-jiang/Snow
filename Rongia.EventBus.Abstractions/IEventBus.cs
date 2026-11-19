namespace Rongia.EventBus;

/// <summary>
/// 事件总线入口。业务层只注入这个接口,不感知事件的传输方式(进程内派发 / 消息队列)。
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// 发布事件,交由所有订阅者处理
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="event">事件实例</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : IEvent;
}

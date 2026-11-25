namespace Snow.EventBus;

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

    /// <summary>
    /// 按运行期类型发布事件: 供工作单元等"事件对象在编译期只知道 object"的场景使用
    /// </summary>
    /// <param name="eventType">事件类型, 必须与 <paramref name="eventData"/> 的实际类型一致</param>
    /// <param name="eventData">事件实例, 必须实现 <see cref="IEvent"/></param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步任务</returns>
    Task PublishAsync(Type eventType, object eventData, CancellationToken cancellationToken = default);
}

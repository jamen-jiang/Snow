using System.Collections.Generic;

namespace Rongia.EventBus;

/// <summary>
/// 事件分发策略: 决定同一事件的多个处理器是顺序执行还是并发执行
/// </summary>
/// <remarks>
/// 默认实现为 <c>ForeachAwaitEventHandlerPublisher</c>(逐个 await, 顺序执行); 需要并发时换成 <c>TaskWhenAllEventHandlerPublisher</c>。
/// </remarks>
public interface IEventHandlerPublisher
{
    /// <summary>
    /// 把事件分发给一批处理器执行器
    /// </summary>
    /// <param name="handlerExecutors">该事件解析出的处理器执行器</param>
    /// <param name="event">被发布的事件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>表示分发过程的任务</returns>
    Task PublishAsync(IEnumerable<EventHandlerExecutor> handlerExecutors, IEvent @event,
        CancellationToken cancellationToken);
}

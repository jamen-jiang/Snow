using System.Collections.Generic;
using System.Linq;

namespace Rongia.EventBus.EventHandlers;

/// <summary>
/// 并发执行全部事件处理器并统一等待:
/// <code>
/// var tasks = handlers
///                .Select(handler => handler.HandleAsync(event, cancellationToken))
///                .ToList();
///
/// return Task.WhenAll(tasks);
/// </code>
/// 任一处理器抛出的异常会在全部任务结束后作为聚合异常抛出。
/// </summary>
public sealed class TaskWhenAllEventHandlerPublisher : IEventHandlerPublisher
{
    /// <inheritdoc />
    public Task PublishAsync(IEnumerable<EventHandlerExecutor> handlerExecutors, IEvent @event,
        CancellationToken cancellationToken)
    {
        var tasks = handlerExecutors
            .Select(handler => handler.HandlerCallback(@event, cancellationToken))
            .ToArray();

        return Task.WhenAll(tasks);
    }
}

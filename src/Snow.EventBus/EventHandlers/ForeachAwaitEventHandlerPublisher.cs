using System.Collections.Generic;
using Snow.DependencyInjection;

namespace Snow.EventBus.EventHandlers;

/// <summary>
/// 逐个 await 事件处理器:
/// <code>
/// foreach (var handler in handlers) {
///     await handler(notification, cancellationToken);
/// }
/// </code>
/// 因此处理器串行执行, 任一失败会中断后续处理器。这是默认策略。
/// </summary>
[SingletonService]
public sealed class ForeachAwaitEventHandlerPublisher : IEventHandlerPublisher
{
    /// <inheritdoc />
    public async Task PublishAsync(IEnumerable<EventHandlerExecutor> handlerExecutors, IEvent @event,
        CancellationToken cancellationToken)
    {
        foreach (var handler in handlerExecutors)
        {
            await handler.HandlerCallback(@event, cancellationToken).ConfigureAwait(false);
        }
    }
}

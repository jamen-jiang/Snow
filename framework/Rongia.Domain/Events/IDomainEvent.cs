using Rongia.Domain.Entities;
using Rongia.EventBus;

namespace Rongia.Domain.Events;

/// <summary>
/// 领域事件接口(对应 Java DomainEvent 抽象类的对外形态)
/// </summary>
/// <remarks>
/// 继承 <see cref="IEvent"/>,让领域事件可以直接交给事件总线派发;
/// 这里只依赖 <c>Rongia.EventBus.Abstractions</c> 的标记接口,领域层不感知任何派发实现。
/// </remarks>
public interface IDomainEvent : IEvent
{
    /// <summary>
    /// 事件类型(类名)
    /// </summary>
    string EventType { get; }

    /// <summary>
    /// 时间戳
    /// </summary>
    DateTime Timestamp { get; }

    /// <summary>
    /// 事件来源聚合根
    /// </summary>
    IAggregateRoot AggregateRoot { get; }
}

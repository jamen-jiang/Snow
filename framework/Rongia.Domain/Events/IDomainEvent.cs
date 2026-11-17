using Rongia.Domain.Aggregates;

namespace Rongia.Domain.Events;

/// <summary>
/// 领域事件接口(对应 Java DomainEvent 抽象类的对外形态)
/// </summary>
public interface IDomainEvent
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

using Rongia.Domain.Aggregates;

namespace Rongia.Domain.Events;

/// <summary>
/// 领域事件基类(对应 Java DomainEvent)
/// </summary>
public abstract class DomainEvent : IDomainEvent
{
    /// <summary>
    /// 初始化领域事件,记录来源聚合根、事件类型与当前时间
    /// </summary>
    protected DomainEvent(IAggregateRoot aggregateRoot)
    {
        AggregateRoot = aggregateRoot;
        EventType = GetType().Name;
        Timestamp = DateTime.Now;
    }

    /// <inheritdoc />
    public string EventType { get; }

    /// <inheritdoc />
    public DateTime Timestamp { get; }

    /// <inheritdoc />
    public IAggregateRoot AggregateRoot { get; }
}

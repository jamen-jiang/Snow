using Rongia.Domain.Entities;
using Rongia.Domain.Events;

namespace Rongia.Domain.Aggregates;

/// <summary>
/// 聚合根接口(对应 Java AggregateRoot)
/// </summary>
public interface IAggregateRoot : IEntity
{
    /// <summary>
    /// 获取当前聚合根产生的所有领域事件(只读)
    /// </summary>
    IReadOnlyList<IDomainEvent> DomainEvents { get; }

    /// <summary>
    /// 添加领域事件,将在工作单元提交时分发
    /// </summary>
    void AddDomainEvent(IDomainEvent @event);

    /// <summary>
    /// 移除指定领域事件
    /// </summary>
    void RemoveDomainEvent(IDomainEvent @event);

    /// <summary>
    /// 清除所有领域事件
    /// </summary>
    void ClearDomainEvents();
}

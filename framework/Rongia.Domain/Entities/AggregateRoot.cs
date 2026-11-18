using Rongia.Domain.Events;

namespace Rongia.Domain.Entities;

/// <summary>
/// 聚合根实体基类(对应 Java BaseAggregateRoot)
/// </summary>
public abstract class AggregateRoot : Entity, IAggregateRoot
{
    /// <summary>
    /// 领域事件列表
    /// </summary>
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <inheritdoc />
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <inheritdoc />
    public void AddDomainEvent(IDomainEvent @event)
    {
        _domainEvents.Add(@event);
    }

    /// <inheritdoc />
    public void RemoveDomainEvent(IDomainEvent @event)
    {
        _domainEvents.Remove(@event);
    }

    /// <inheritdoc />
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}

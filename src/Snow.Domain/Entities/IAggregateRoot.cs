using Snow.Domain.Events;

namespace Snow.Domain.Entities;

/// <summary>
/// 聚合根接口。聚合根的主键不一定叫 "Id",也可能是复合主键。
/// 仓储等框架组件尽量面向 <see cref="IAggregateRoot"/> 编程,以获得更好的集成能力。
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
    /// <param name="event">待添加的领域事件</param>
    void AddDomainEvent(IDomainEvent @event);

    /// <summary>
    /// 移除指定领域事件
    /// </summary>
    /// <param name="event">待移除的领域事件</param>
    void RemoveDomainEvent(IDomainEvent @event);

    /// <summary>
    /// 清除所有领域事件
    /// </summary>
    void ClearDomainEvents();
}

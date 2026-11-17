namespace Rongia.Domain.Events;

/// <summary>
/// 领域事件发布器接口。Domain 层只依赖这个抽象,不感知任何具体框架(对应 Java DomainEventPublisher)。
/// </summary>
public interface IDomainEventPublisher
{
    /// <summary>
    /// 发布领域事件
    /// </summary>
    void Publish(DomainEvent @event);
}

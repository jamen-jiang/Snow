namespace Rongia.Domain.Events;

/// <summary>
/// 领域事件监听特性,打在方法上(对应 Java @DomainEventHandler 注解)。
/// 方法必须只有一个参数,且参数类型是 DomainEvent 的子类。
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class DomainEventHandlerAttribute : Attribute
{
}

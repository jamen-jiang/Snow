using Snow.Domain.Entities;

namespace Snow.Domain.Events;

/// <summary>
/// 添加审计日志事件(对应 Java AddAuditLogEvent)
/// </summary>
public class AddAuditLogEvent : DomainEvent
{
    /// <summary>
    /// 动作
    /// </summary>
    public string Action { get; }

    /// <summary>
    /// 备注(可空)
    /// </summary>
    public string? Remark { get; }

    /// <summary>
    /// 构造审计日志事件(带备注)
    /// </summary>
    public AddAuditLogEvent(IAggregateRoot aggregateRoot, string action, string? remark)
        : base(aggregateRoot)
    {
        Action = action;
        Remark = remark;
    }

    /// <summary>
    /// 构造审计日志事件(备注为空)
    /// </summary>
    public AddAuditLogEvent(IAggregateRoot aggregateRoot, string action)
        : this(aggregateRoot, action, null) { }
}

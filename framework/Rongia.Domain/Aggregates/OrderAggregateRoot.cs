using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Rongia.Domain.Enums;
using Rongia.Domain.Events;
using Rongia.Domain.Exceptions;

namespace Rongia.Domain.Aggregates;

/// <summary>
/// 订单聚合根基类(对应 Java OrderAggregateRoot)
/// </summary>
public abstract class OrderAggregateRoot : UpdateAggregateRoot
{
    /// <summary>
    /// 单号
    /// </summary>
    [Column("Number"), MaxLength(100)]
    public string Number { get; protected set; } = string.Empty;

    /// <summary>
    /// 无参构造器(供框架反射使用)
    /// </summary>
    protected OrderAggregateRoot() { }

    /// <summary>
    /// 业务创建入口:必须带单号
    /// </summary>
    protected OrderAggregateRoot(string number)
    {
        AssignNumber(number);
    }

    /// <summary>
    /// 获取流水号类型:本单据在流水号规则里的类型
    /// </summary>
    public abstract SerialNumberType GetSerialNumberType();

    /// <summary>
    /// 分配单号,只能分配一次
    /// </summary>
    public void AssignNumber(string number)
    {
        if (!string.IsNullOrWhiteSpace(Number))
        {
            throw new DomainException("已分配单号,不能重复分配!");
        }
        Number = number;
        AddDomainEvent(new OrderAddedEvent(this));
    }

    /// <summary>
    /// 记录审计动作(备注为空)
    /// </summary>
    public void Audit(string action)
    {
        Audit(action, null);
    }

    /// <summary>
    /// 记录审计动作与备注
    /// </summary>
    public void Audit(string action, string? remark)
    {
        AddDomainEvent(new AddAuditLogEvent(this, action, remark));
    }
}

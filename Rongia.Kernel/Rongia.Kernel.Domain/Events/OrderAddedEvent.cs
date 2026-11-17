using Rongia.Kernel.Domain.Aggregates;

namespace Rongia.Kernel.Domain.Events;

/// <summary>
/// 订单新增事件,用于生成流水号(对应 Java OrderAddedEvent)
/// </summary>
public class OrderAddedEvent : DomainEvent
{
    /// <summary>
    /// 订单
    /// </summary>
    public OrderAggregateRoot Order { get; }

    /// <summary>
    /// 构造订单新增事件
    /// </summary>
    public OrderAddedEvent(OrderAggregateRoot order) : base(order)
    {
        Order = order;
    }

    /// <summary>
    /// 流水号类型编码(对应 SerialNumberType 成员名,流水号规则按它取前缀)。
    /// <para>到用的时候才问单据,不在这里存字符串:本事件是基类构造器里建出来的,那会儿子类自己的字段还没归位,
    /// 类型又是由单据自己的字段算出来的。事件是 save 之后才发布的,那时单据已经装配齐。</para>
    /// </summary>
    public string Type => Order.GetSerialNumberType().ToString();
}

using System.ComponentModel;

namespace Snow.Domain.Enums;

/// <summary>
/// 流水号类型枚举(对应 Java SerialNumberTypeEnum;成员名即 Java 的 code,标签在 Description)。
/// <para>编码沿用实体表名(WorkOrder / StockEntry),与 OrderAddedEvent 里带出去的类型串一致,
/// 流水号规则按这个编码挂单号前缀与编号段。一张表一个编码,表里的红字单、普通单共用一个号段。</para>
/// </summary>
public enum SerialNumberType
{
    /// <summary>
    /// 销售订单
    /// </summary>
    [Description("销售订单")]
    SalesOrder,

    /// <summary>
    /// 采购单
    /// </summary>
    [Description("采购单")]
    PurchaseOrder,

    /// <summary>
    /// 工单(生产订单)
    /// </summary>
    [Description("工单")]
    WorkOrder,

    /// <summary>
    /// 工卡(作业卡,一张卡对应工单上的一道工序)
    /// </summary>
    [Description("工卡")]
    JobCard,

    /// <summary>
    /// 库存单据(通用出入库单,红字单也用这一段号)
    /// </summary>
    [Description("库存单据")]
    StockEntry,
}

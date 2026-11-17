using System.ComponentModel;

namespace Rongia.Kernel.Domain.Enums;

/// <summary>
/// 数据类型(对应 Java DataTypeEnum;成员名即 Java 的 code,标签在 Description)
/// </summary>
public enum DataType
{
    /// <summary>
    /// 字符串
    /// </summary>
    [Description("字符串")]
    String,

    /// <summary>
    /// 整数
    /// </summary>
    [Description("整数")]
    Integer,

    /// <summary>
    /// 小数
    /// </summary>
    [Description("小数")]
    Decimal,

    /// <summary>
    /// 布尔值
    /// </summary>
    [Description("布尔值")]
    Boolean,

    /// <summary>
    /// 日期
    /// </summary>
    [Description("日期")]
    DateTime,
}

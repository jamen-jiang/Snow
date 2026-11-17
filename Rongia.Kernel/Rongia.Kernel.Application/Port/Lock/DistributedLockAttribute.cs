namespace Rongia.Kernel.Application.Port.Lock;

/// <summary>
/// 分布式锁特性(AOP 拦截器读取;具体加锁实现由基础设施层提供,如 Redisson → RedLock)
/// <para>
/// 用法:
/// <code>
/// // 只用 key
/// [DistributedLock("'Stock_' + #variantId + '_' + #warehouseId")]
///
/// // prefix + key
/// [DistributedLock(Prefix = "Stock", Value = "#variantId + '_' + #warehouseId")]
///
/// // prefix + key + condition
/// [DistributedLock(Prefix = "Stock", Value = "#variantId", Condition = "#qty > 0")]
/// </code>
/// </para>
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class DistributedLockAttribute : Attribute
{
    /// <summary>
    /// 锁 key 前缀,与 Value 拼接(用 ":" 分隔)
    /// </summary>
    public string Prefix { get; set; } = "";

    /// <summary>
    /// 锁 key 表达式,为空时用「类名_方法名」
    /// </summary>
    public string Value { get; set; } = "";

    /// <summary>
    /// 条件表达式,为 false 时跳过加锁
    /// </summary>
    public string Condition { get; set; } = "";
}

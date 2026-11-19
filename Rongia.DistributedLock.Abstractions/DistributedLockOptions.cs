namespace Rongia.DistributedLock;

/// <summary>
/// 分布式锁配置(对应 ABP 的 AbpDistributedLockOptions)
/// <para>由使用方通过 <c>context.Services.Configure&lt;DistributedLockOptions&gt;(...)</c> 设置, 默认无前缀。</para>
/// </summary>
public class DistributedLockOptions
{
    /// <summary>
    /// 锁 key 的统一前缀,默认空字符串;用于给不同应用/租户的锁隔离命名空间
    /// </summary>
    public string KeyPrefix { get; set; }

    /// <summary>
    /// 构造器:前缀默认为空字符串
    /// </summary>
    public DistributedLockOptions()
    {
        KeyPrefix = "";
    }
}

namespace Snow.DistributedLocking;

/// <summary>
/// 分布式锁配置(对应 ABP 的 <c>AbpDistributedLockOptions</c>): 只有 key 前缀一项。
/// <para>由使用方在模块里 <c>context.Services.Configure&lt;DistributedLockOptions&gt;(o => o.KeyPrefix = "app:")</c> 设置,
/// 与 ABP 一致, 不绑配置节。</para>
/// <para>租期、续期节奏、抢锁重试间隔都是实现内部的常量(<see cref="DistributedLockWatchdog.LeaseTime"/> 等),
/// 不开放配置; 需要「到点自动释放」就在这一次抢锁时传 <c>leaseTime</c>。</para>
/// </summary>
public class DistributedLockOptions
{
    /// <summary>
    /// 构造器: 前缀为空
    /// </summary>
    public DistributedLockOptions()
    {
        KeyPrefix = "";
    }

    /// <summary>
    /// 锁 key 的统一前缀, 默认空字符串; 用于给不同应用/租户的锁隔离命名空间
    /// </summary>
    public string KeyPrefix { get; set; }

    /// <summary>
    /// 把业务锁名拼成最终 key: 前缀 + 用 ":" 分隔的业务名(前缀已以 ":" 结尾时不重复补)
    /// </summary>
    /// <param name="name">业务传入的锁名称</param>
    /// <returns>最终锁 key</returns>
    public virtual string ResolveKey(string name)
    {
        if (string.IsNullOrEmpty(KeyPrefix))
        {
            return name;
        }

        return KeyPrefix.EndsWith(':') ? KeyPrefix + name : KeyPrefix + ":" + name;
    }
}

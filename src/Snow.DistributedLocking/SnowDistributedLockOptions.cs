namespace Snow.DistributedLocking;

/// <summary>
/// 分布式锁全局配置(对应 ABP 的 <c>AbpDistributedLockOptions</c>): 只有 key 前缀一项。
/// <para>由使用方在模块里 <c>context.Services.Configure&lt;SnowDistributedLockOptions&gt;(...)</c> 设置, 与 ABP 一致不绑配置节。</para>
/// <para>前缀怎么叠加成 key 不在这里做, 由 <see cref="IDistributedLockKeyNormalizer"/> 负责(与 ABP 同构)。</para>
/// </summary>
public class SnowDistributedLockOptions
{
    /// <summary>
    /// 构造器: 前缀为空
    /// </summary>
    public SnowDistributedLockOptions()
    {
        KeyPrefix = "";
    }

    /// <summary>
    /// 锁 key 的统一前缀, 默认空字符串; 用于给不同应用/租户的锁隔离命名空间
    /// </summary>
    public string KeyPrefix { get; set; }
}
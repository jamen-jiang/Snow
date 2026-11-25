namespace Snow.DistributedLocking;

/// <summary>
/// Redis 分布式锁的配置: 目前只有连接串一项, 绑定配置节 <c>Redis</c>(与缓存、会话共用同一节)。
/// <para>连接串通过 <c>context.Services.Configure&lt;SnowDistributedLockRedisOptions&gt;(...)</c> 或
/// <c>Configure&lt;SnowDistributedLockRedisOptions&gt;(context.Configuration.GetSection("Redis"))</c> 传入;
/// 缺省 <c>127.0.0.1:6379</c>。</para>
/// </summary>
public class SnowDistributedLockRedisOptions
{
    /// <summary>
    /// StackExchange.Redis 连接串, 默认本机
    /// </summary>
    public string Configuration { get; set; } = "127.0.0.1:6379";
}

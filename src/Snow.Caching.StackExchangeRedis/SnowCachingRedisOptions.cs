namespace Snow.Caching.StackExchangeRedis;

/// <summary>
/// Redis 缓存的连接选项(对应 ABP 的 <c>AbpRedisCacheOptions</c> 里只管连接串的那部分)
/// <para>模块不读配置节: 宿主在 <c>PreConfigureServices</c> 里
/// <c>Configure&lt;SnowCachingRedisOptions&gt;(context.Configuration.GetSection("Redis"))</c> 或直接改属性。</para>
/// </summary>
public class SnowCachingRedisOptions
{
    /// <summary>
    /// 连接串(StackExchange.Redis 格式), 默认 <c>127.0.0.1:6379</c>
    /// </summary>
    public string? Configuration { get; set; } = "127.0.0.1:6379";
}

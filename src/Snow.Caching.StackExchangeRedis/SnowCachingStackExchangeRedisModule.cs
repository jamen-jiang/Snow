using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Snow.DependencyInjection;
using Snow.Modularity;
using BclDistributedCache = Microsoft.Extensions.Caching.Distributed.IDistributedCache;

namespace Snow.Caching.StackExchangeRedis;

/// <summary>
/// Redis 缓存后端模块: 用 Redis 承载 BCL 的字节缓存, 覆盖 <see cref="SnowCachingModule"/> 登记的进程内兜底
/// <para>是否启用由模块图决定: 启动模块写了 <c>[DependsOn(typeof(SnowCachingStackExchangeRedisModule))]</c> 就启用,
/// 不写则本模块的 <see cref="ConfigureServices"/> 根本不会执行, 缓存保持进程内实现。没有配置开关。</para>
/// <para>连接串取 <c>Redis:Configuration</c>, 缺省 <c>127.0.0.1:6379</c>, 与 <c>Snow.DistributedLocking.Redis</c> 用同一节同一默认值。
/// 连不上时不会阻断启动: 底层 <see cref="RedisCache"/> 建连时强制 <c>abortConnect=false</c>, 首次访问再重试。</para>
/// </summary>
[DependsOn(typeof(SnowCachingModule))]
public sealed class SnowCachingStackExchangeRedisModule : SnowModule
{
    /// <summary>
    /// 缺省连接串, 与 <c>Snow.DistributedLocking.Redis</c> 保持一致
    /// </summary>
    private const string DefaultConfiguration = "127.0.0.1:6379";

    /// <inheritdoc />
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        IConfiguration configuration = context.Services.GetConfiguration();

        // 必须给非空连接串: RedisCacheOptions.GetConfiguredOptions() 在 Configuration 与 ConfigurationOptions 都为 null 时
        // 会去 Parse(null) 抛 ArgumentNullException, 表现为首次访问缓存直接异常
        string? configured = configuration["Redis:Configuration"];
        string connectionString = string.IsNullOrWhiteSpace(configured) ? DefaultConfiguration : configured;

        context.Services.AddStackExchangeRedisCache(options => options.Configuration = connectionString);

        // SnowCachingModule 已登记进程内 MemoryDistributedCache, 这里替换为 Redis 实现
        context.Services.Replace(ServiceDescriptor.Singleton<BclDistributedCache, RedisCache>());
    }
}

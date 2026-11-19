using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Rongia.Modularity;
using BclDistributedCache = Microsoft.Extensions.Caching.Distributed.IDistributedCache;

namespace Rongia.Caching.StackExchangeRedis;

/// <summary>
/// Redis 缓存后端模块: 用 Redis 承载 BCL 的字节缓存, 覆盖 <see cref="RongiaCachingModule"/> 登记的进程内兜底
/// <para>配置来自 <c>Redis</c> 节: <c>IsEnabled</c> 缺省视为开启, <c>Configuration</c> 为连接串; 关闭时保持进程内缓存。</para>
/// </summary>
[DependsOn(typeof(RongiaCachingModule))]
public sealed class RongiaCachingStackExchangeRedisModule : RongiaModule
{
    /// <inheritdoc />
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        IConfiguration configuration = context.Configuration;

        string? redisEnabled = configuration["Redis:IsEnabled"];
        if (!string.IsNullOrEmpty(redisEnabled) && !bool.Parse(redisEnabled))
        {
            return;
        }

        context.Services.AddStackExchangeRedisCache(options =>
        {
            string? redisConfiguration = configuration["Redis:Configuration"];
            if (!string.IsNullOrEmpty(redisConfiguration))
            {
                options.Configuration = redisConfiguration;
            }
        });

        // RongiaCachingModule 已登记进程内 MemoryDistributedCache, 这里替换为 Redis 实现
        context.Services.Replace(ServiceDescriptor.Singleton<BclDistributedCache, RedisCache>());
    }
}

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Rongia.Modularity;

namespace Rongia.Caching;

/// <summary>
/// 分布式缓存模块: 注册默认进程内缓存(宿主接 Redis 时自动被替换)与强类型缓存门面
/// </summary>
public sealed class RongiaCachingModule : RongiaModule
{
    /// <inheritdoc />
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // 进程内兜底: 宿主已注册 IDistributedCache(如 Redis)时 TryAdd 不会覆盖
        context.Services.AddMemoryCache();
        context.Services.AddDistributedMemoryCache();

        // 开放泛型强类型缓存: 约定扫描会跳过开放泛型, 必须显式登记
        context.Services.AddSingleton(typeof(IDistributedCache<>), typeof(DistributedCache<>));
        context.Services.AddSingleton(typeof(IDistributedCache<,>), typeof(DistributedCache<,>));

        // 配置节: DistributedCache:{ KeyPrefix, HideErrors, GlobalCacheEntryOptions }
        IConfigurationSection section = context.Configuration.GetSection(DistributedCacheOptions.SectionName);
        if (section.Exists())
        {
            context.Services.Configure<DistributedCacheOptions>(section);
        }

        // 默认滑动过期 20 分钟(与 ABP 一致), 配置显式指定时不覆盖
        context.Services.PostConfigure<DistributedCacheOptions>(options =>
        {
            if (options.GlobalCacheEntryOptions.SlidingExpiration is null && options.GlobalCacheEntryOptions.AbsoluteExpiration is null && options.GlobalCacheEntryOptions.AbsoluteExpirationRelativeToNow is null)
            {
                options.GlobalCacheEntryOptions.SlidingExpiration = TimeSpan.FromMinutes(20);
            }
        });
    }
}

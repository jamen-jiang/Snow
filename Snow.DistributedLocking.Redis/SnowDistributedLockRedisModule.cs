using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Snow.Modularity;
using StackExchange.Redis;

namespace Snow.DistributedLocking;

/// <summary>
/// 分布式锁的 Redis 模块: 依赖 <see cref="SnowDistributedLockModule"/>,
/// 注册 Redis 连接并把默认锁顶替成 <see cref="RedisDistributedLock"/>。
/// <para>连接串取配置 <c>Redis:Configuration</c>(与缓存、会话共用同一节), 缺省 <c>127.0.0.1:6379</c>;
/// 宿主已注册 <see cref="IConnectionMultiplexer"/> 时直接复用, 不会重复建连。</para>
/// <para>租期与续期节奏是常量(<see cref="DistributedLockWatchdog.LeaseTime"/>),
/// <see cref="DistributedLockOptions"/> 里只有 key 前缀一项。</para>
/// </summary>
[DependsOn(typeof(SnowDistributedLockModule))]
public sealed class SnowDistributedLockRedisModule : SnowModule
{
    /// <inheritdoc />
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        base.ConfigureServices(context);

        context.Services.TryAddSingleton<IConnectionMultiplexer>(provider =>
        {
            IConfiguration configuration = provider.GetRequiredService<IConfiguration>();
            string configurationString = configuration["Redis:Configuration"] ?? "127.0.0.1:6379";

            return ConnectionMultiplexer.Connect(EnsureAbortConnectFalse(configurationString));
        });
    }

    /// <summary>
    /// 补上 abortConnect=false: 启动时 Redis 没起来也不让整个应用崩, 后续请求再重试
    /// </summary>
    /// <param name="configurationString">原始连接串</param>
    /// <returns>补全后的连接串</returns>
    private static string EnsureAbortConnectFalse(string configurationString)
    {
        return configurationString.Contains("abortConnect", StringComparison.OrdinalIgnoreCase)
            ? configurationString
            : configurationString + ",abortConnect=false";
    }
}

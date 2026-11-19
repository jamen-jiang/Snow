using Medallion.Threading;
using Medallion.Threading.Redis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Rongia.Aop.Castle;
using Rongia.Authorization;
using Rongia.Authorization.Permissions;
using Rongia.Caching;
using Rongia.Caching.StackExchangeRedis;
using Rongia.DependencyInjection;
using Rongia.DistributedLock;
using Rongia.EventBus;
using Rongia.Messaging;
using Rongia.Modularity;
using Rongia.Permissions;
using Rongia.Security.SaToken;
using StackExchange.Redis;

namespace Rongia;

/// <summary>
/// 宿主启动模块: 组合根, 声明本应用由哪些模块组成, 并完成只有宿主才知道的部署接线
/// </summary>
[DependsOn(
    typeof(MessagingModule),
    typeof(EventBusModule),
    typeof(AopModule),
    typeof(DistributedLockModule),
    typeof(AuthorizationModule),
    typeof(CachingModule),
    typeof(CachingStackExchangeRedisModule),
    typeof(SaTokenModule))]
public sealed class RongiaModule : Module
{
    /// <inheritdoc />
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        IConfiguration configuration = context.Services.GetConfiguration();

        ConfigureDistributedLocking(context, configuration);

        // 登记本应用的权限定义提供器(权限树在 RongiaPermissionDefinitionProvider 中声明)
        context.Services.Configure<PermissionOptions>(options =>
        {
            options.DefinitionProviders.Add<RongiaPermissionDefinitionProvider>();
        });
    }

    /// <summary>
    /// 为分布式锁注册 Redis 提供器: 契约与实现都在框架包里, 这里只决定"用哪个后端"
    /// </summary>
    /// <param name="context">服务配置上下文</param>
    /// <param name="configuration">配置</param>
    private static void ConfigureDistributedLocking(ServiceConfigurationContext context, IConfiguration configuration)
    {
        context.Services.AddSingleton<IDistributedLockProvider>(serviceProvider =>
        {
            ConnectionMultiplexer connection = ConnectionMultiplexer.Connect(configuration["Redis:Configuration"]!);
            return new RedisDistributedSynchronizationProvider(connection.GetDatabase());
        });
    }
}

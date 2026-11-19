using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Rongia.Clients;
using Rongia.MultiTenancy;
using Rongia.Tracing;

namespace Rongia.Modularity;

/// <summary>
/// 内核基础服务注册(对应 ABP 的 <c>AddCoreAbpServices</c>):
/// 租户/客户端/链路标识的访问器属于框架自身的运行时上下文, 与业务模块无关,
/// 因此由框架在应用创建时直接登记, 不依赖约定注册是否扫描到内核程序集。
/// </summary>
public static class CoreServiceCollectionExtensions
{
    /// <summary>
    /// 登记内核运行时上下文服务(调用方已注册时保留调用方的实现)
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合本身</returns>
    public static IServiceCollection AddCoreRongiaServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // 租户上下文
        services.TryAddSingleton<ICurrentTenantAccessor, CurrentTenantAccessor>();
        services.TryAddTransient<ICurrentTenant, CurrentTenant>();

        // 客户端上下文
        services.TryAddSingleton<ICurrentClientAccessor, CurrentClientAccessor>();
        services.TryAddTransient<ICurrentClient, CurrentClient>();

        // 链路标识
        services.TryAddSingleton<ICorrelationIdProvider, CorrelationIdProvider>();

        return services;
    }
}

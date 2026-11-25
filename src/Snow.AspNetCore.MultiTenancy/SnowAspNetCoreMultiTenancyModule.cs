using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Snow.AspNetCore;
using Snow.Modularity;
using Snow.MultiTenancy;

namespace Snow.AspNetCore.MultiTenancy;

/// <summary>
/// 多租户 Web 适配模块(对齐 ABP 的 <c>AbpAspNetCoreMultiTenancyModule</c>)
/// <para>登记四个请求侧出资者, 顺序与 ABP 一致: QueryString → Route → Header → Cookie;
/// 当前用户出资者已在 <see cref="SnowMultiTenancyModule"/> 里排在最前面。</para>
/// <para>租户解析中间件由宿主自己挂: 在启动模块的 OnApplicationInitialization 里调
/// <c>context.GetApplicationBuilder().UseSnowMultiTenancy()</c>, 放在认证之后、授权之前 ——
/// 它的"当前用户"出资者要读已认证主体的租户声明, 放认证之前就只能拿到匿名请求, 会静默回落到 Host 租户。</para>
/// </summary>
[DependsOn(typeof(SnowMultiTenancyModule), typeof(SnowAspNetCoreModule))]
public sealed class SnowAspNetCoreMultiTenancyModule : SnowModule
{
    /// <inheritdoc />
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.Services.TryAddEnumerable(ServiceDescriptor.Transient<ITenantResolveContributor, QueryStringTenantResolveContributor>());
        context.Services.TryAddEnumerable(ServiceDescriptor.Transient<ITenantResolveContributor, RouteTenantResolveContributor>());
        context.Services.TryAddEnumerable(ServiceDescriptor.Transient<ITenantResolveContributor, HeaderTenantResolveContributor>());
        context.Services.TryAddEnumerable(ServiceDescriptor.Transient<ITenantResolveContributor, CookieTenantResolveContributor>());
    }

}

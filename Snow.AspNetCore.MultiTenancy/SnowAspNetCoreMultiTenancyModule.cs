using Microsoft.Extensions.Configuration;
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
/// <para>中间件由本模块登记进管道清单, 排在路由与认证之后、授权之前, 宿主不用写 <c>UseSnowMultiTenancy()</c>:
/// 路由出资者要读路由值, 当前用户出资者要读已认证用户。</para>
/// </summary>
[DependsOn(typeof(SnowMultiTenancyModule), typeof(SnowAspNetCoreModule))]
public sealed class SnowAspNetCoreMultiTenancyModule : SnowModule
{
    /// <inheritdoc />
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Configuration.GetSection("MultiTenancy:AspNetCore").Exists())
        {
            Configure<SnowAspNetCoreMultiTenancyOptions>(context.Configuration.GetSection("MultiTenancy:AspNetCore"));
        }

        context.Services.TryAddEnumerable(ServiceDescriptor.Transient<ITenantResolveContributor, QueryStringTenantResolveContributor>());
        context.Services.TryAddEnumerable(ServiceDescriptor.Transient<ITenantResolveContributor, RouteTenantResolveContributor>());
        context.Services.TryAddEnumerable(ServiceDescriptor.Transient<ITenantResolveContributor, HeaderTenantResolveContributor>());
        context.Services.TryAddEnumerable(ServiceDescriptor.Transient<ITenantResolveContributor, CookieTenantResolveContributor>());

        // 认证之后、授权之前: 路由出资者要读路由值, 当前用户出资者要读已认证用户
        context.Services.AddSnowMiddleware<MultiTenancyMiddleware>(SnowPipelineStage.AfterAuthentication);
    }
}

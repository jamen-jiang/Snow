using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Snow.Modularity;

namespace Snow.AspNetCore;

/// <summary>
/// 端点统一挂载
/// <para>各模块用 <c>AddSnowEndpoint(...)</c> 把映射动作登记进 <see cref="SnowPipelineRegistry"/>,
/// Web 宿主下由 <see cref="SnowAspNetCoreModule"/> 在应用初始化阶段自动执行, 宿主一行都不用写。</para>
/// </summary>
public static class SnowEndpointApplicationBuilderExtensions
{
    private const string AppliedKey = "Snow.Endpoints.Applied";

    /// <summary>
    /// 判断端点配置动作是否已经执行过
    /// </summary>
    /// <param name="app">应用构建器</param>
    /// <returns>已执行返回 true</returns>
    public static bool IsSnowEndpointsApplied(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.Properties.ContainsKey(AppliedKey);
    }

    /// <summary>
    /// 执行各模块登记的端点映射动作(只执行一次)
    /// <para>Web 宿主(<c>WebApplication</c>)下这一步由 <c>SnowAspNetCoreModule</c> 自动完成;
    /// 本方法保留给通用主机与需要追加端点的宿主, 重复调用不会重复映射。</para>
    /// </summary>
    /// <param name="app">应用构建器</param>
    /// <param name="additionalConfigurationAction">额外的端点配置动作</param>
    /// <returns>应用构建器本身</returns>
    public static IApplicationBuilder UseSnowEndpoints(
        this IApplicationBuilder app,
        Action<IEndpointRouteBuilder>? additionalConfigurationAction = null)
    {
        ArgumentNullException.ThrowIfNull(app);

        if (app.IsSnowEndpointsApplied())
        {
            // 已经映射过: 只追加宿主这一次的额外动作
            if (additionalConfigurationAction is not null && app is IEndpointRouteBuilder mapped)
            {
                additionalConfigurationAction(mapped);
            }

            return app;
        }

        return ApplyEndpoints(app, additionalConfigurationAction, markApplied: true);
    }

    /// <summary>
    /// 自动挂载端点: 只在 Web 宿主下生效, 由模块在应用初始化阶段调用
    /// </summary>
    /// <param name="app">应用构建器</param>
    /// <returns>应用构建器本身</returns>
    internal static IApplicationBuilder UseSnowEndpointsIfWebHost(this IApplicationBuilder app)
    {
        if (app is not IEndpointRouteBuilder)
        {
            // 通用主机: 端点要在宿主自己调 UseRouting/UseEndpoints 的位置挂, 这里不插手
            return app;
        }

        if (app.IsSnowEndpointsApplied())
        {
            return app;
        }

        return ApplyEndpoints(app, null, markApplied: true);
    }

    private static IApplicationBuilder ApplyEndpoints(
        IApplicationBuilder app,
        Action<IEndpointRouteBuilder>? additionalConfigurationAction,
        bool markApplied)
    {
        IReadOnlyList<Action<IEndpointRouteBuilder>> endpoints = app.ApplicationServices
            .GetService<SnowPipelineRegistry>()?
            .Endpoints ?? [];

        if (endpoints.Count == 0 && additionalConfigurationAction is null)
        {
            return app;
        }

        if (markApplied)
        {
            app.Properties[AppliedKey] = true;
        }

        // WebApplication 自身就是 IEndpointRouteBuilder, 直接往上映射即可, 不需要 UseRouting + UseEndpoints 那一套
        if (app is IEndpointRouteBuilder endpointRouteBuilder)
        {
            Apply(app, endpointRouteBuilder, endpoints, additionalConfigurationAction);
            return app;
        }

        return app.UseEndpoints(builder => Apply(app, builder, endpoints, additionalConfigurationAction));
    }

    private static void Apply(
        IApplicationBuilder app,
        IEndpointRouteBuilder endpoints,
        IReadOnlyList<Action<IEndpointRouteBuilder>> configureActions,
        Action<IEndpointRouteBuilder>? additionalConfigurationAction)
    {
        foreach (Action<IEndpointRouteBuilder> configure in configureActions)
        {
            configure(endpoints);
        }

        additionalConfigurationAction?.Invoke(endpoints);
    }
}

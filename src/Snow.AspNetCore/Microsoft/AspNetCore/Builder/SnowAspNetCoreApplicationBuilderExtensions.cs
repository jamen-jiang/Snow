using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Snow.AspNetCore;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// 端点映射入口(对应 ABP 的 <c>UseConfiguredEndpoints()</c>, 同在 <c>Microsoft.AspNetCore.Builder</c> 命名空间)
/// <para>各模块在服务注册阶段把映射动作交进 <c>SnowEndpointRouterOptions</c>, 宿主在管道最后一步调本方法统一执行。</para>
/// </summary>
public static class SnowAspNetCoreApplicationBuilderExtensions
{
    /// <summary>
    /// 映射各模块登记的端点: 放在管道最后一步(所有中间件之后)
    /// <para>与 ABP 的 <c>UseConfiguredEndpoints()</c> 同形: 动作全空且宿主没传额外动作时原样返回, 一个中间件都不加。</para>
    /// </summary>
    /// <param name="app">应用构建器</param>
    /// <param name="additionalConfigurationAction">宿主额外想映射的端点; 可为 null</param>
    /// <returns>应用构建器本身</returns>
    public static IApplicationBuilder UseSnowEndpoints(
        this IApplicationBuilder app,
        Action<IEndpointRouteBuilder>? additionalConfigurationAction = null
    )
    {
        ArgumentNullException.ThrowIfNull(app);

        SnowEndpointRouterOptions options = app
            .ApplicationServices.GetRequiredService<IOptions<SnowEndpointRouterOptions>>()
            .Value;

        if (options.EndpointConfigureActions.Count == 0 && additionalConfigurationAction is null)
        {
            return app;
        }

        // WebApplication 自己就是 IEndpointRouteBuilder: 端点直接进数据源, 不必再包一层 UseEndpoints
        if (app is IEndpointRouteBuilder endpoints)
        {
            MapEndpoints(app.ApplicationServices, endpoints, options, additionalConfigurationAction);
            return app;
        }

        app.UseEndpoints(routeBuilder =>
            MapEndpoints(app.ApplicationServices, routeBuilder, options, additionalConfigurationAction)
        );
        return app;
    }

    /// <summary>
    /// 跑完登记的映射动作, 再跑宿主额外传进来的动作
    /// </summary>
    /// <param name="services">根服务提供者</param>
    /// <param name="endpoints">端点路由构建器</param>
    /// <param name="options">端点路由选项</param>
    /// <param name="additionalConfigurationAction">宿主额外想映射的端点; 可为 null</param>
    private static void MapEndpoints(
        IServiceProvider services,
        IEndpointRouteBuilder endpoints,
        SnowEndpointRouterOptions options,
        Action<IEndpointRouteBuilder>? additionalConfigurationAction
    )
    {
        if (options.EndpointConfigureActions.Count > 0)
        {
            // 独立作用域: 映射动作里解析 Scoped 服务不会捕获根容器
            using IServiceScope scope = endpoints.ServiceProvider.CreateScope();
            SnowEndpointRouteBuilderContext context = new(endpoints, scope.ServiceProvider);

            foreach (
                Action<SnowEndpointRouteBuilderContext> configure in options.EndpointConfigureActions
            )
            {
                configure(context);
            }
        }

        additionalConfigurationAction?.Invoke(endpoints);
    }
}

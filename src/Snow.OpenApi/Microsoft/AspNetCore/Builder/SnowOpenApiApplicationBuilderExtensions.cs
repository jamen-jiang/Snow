using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;
using Snow.DependencyInjection;
using Snow.OpenApi;
using Snow.Modularity;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// OpenAPI 的端点挂载入口
/// </summary>
public static class SnowOpenApiApplicationBuilderExtensions
{
    /// <summary>
    /// 挂上文档端点与 Scalar 调试界面
    /// <para>正常情况不用调: <c>SnowOpenApiModule</c> 已把映射动作声明进端点清单,
    /// 框架在全部模块初始化完成之后统一执行。保留给单独使用本包的宿主。</para>
    /// </summary>
    /// <param name="app">应用构建器</param>
    /// <returns>应用构建器本身</returns>
    public static IApplicationBuilder UseSnowOpenApi(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        SnowOpenApiOptions options = app.ApplicationServices.GetRequiredService<IOptions<SnowOpenApiOptions>>().Value;
        if (!options.Enabled)
        {
            return app;
        }

        IEndpointRouteBuilder? endpoints = app as IEndpointRouteBuilder
            ?? app.ApplicationServices.GetService<IObjectAccessor<IEndpointRouteBuilder>>()?.Value;

        if (endpoints is null)
        {
            throw new SnowInitializationException("找不到端点路由构建器: 需要 Web 宿主, 且启动模块要依赖 SnowAspNetCoreModule。");
        }

        MapOpenApiEndpoints(endpoints, options);

        return app;
    }

    /// <summary>
    /// 映射文档与调试界面端点(模块在服务登记阶段就把它登记进统一挂载清单)
    /// </summary>
    /// <param name="endpoints">端点路由构建器</param>
    /// <param name="options">文档选项</param>
    internal static void MapOpenApiEndpoints(IEndpointRouteBuilder endpoints, SnowOpenApiOptions options)
    {
        string prefix = options.RoutePrefix.Trim('/');

        endpoints.MapOpenApi("/" + prefix + "/{documentName}.json");
        endpoints.MapScalarApiReference(prefix, scalar =>
        {
            scalar.WithTitle(options.Title);
            scalar.WithOpenApiRoutePattern("/" + prefix + "/{documentName}.json");
        });
    }
}
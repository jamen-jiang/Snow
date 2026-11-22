using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;
using Snow.Modularity;

namespace Snow.OpenApi;

/// <summary>
/// OpenAPI 的端点挂载入口
/// </summary>
public static class SnowSwaggerApplicationBuilderExtensions
{
    /// <summary>
    /// 挂上文档端点与 Scalar 调试界面
    /// </summary>
    /// <param name="app">应用构建器</param>
    /// <returns>应用构建器本身</returns>
    public static IApplicationBuilder UseSnowSwagger(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        SnowSwaggerOptions options = app.ApplicationServices.GetRequiredService<IOptions<SnowSwaggerOptions>>().Value;
        if (!options.Enabled)
        {
            return app;
        }

        IEndpointRouteBuilder? endpoints = app as IEndpointRouteBuilder
            ?? app.ApplicationServices.GetService<IObjectAccessor<IEndpointRouteBuilder>>()?.Value;

        if (endpoints is null)
        {
            throw new SnowInitializationException("找不到端点路由构建器: 需要 Web 宿主, 并先调用 UseSnowAspNetCore / InitializeSnowAsync。");
        }

        string prefix = options.RoutePrefix.Trim('/');

        endpoints.MapOpenApi("/" + prefix + "/{documentName}.json");
        endpoints.MapScalarApiReference(prefix, scalar =>
        {
            scalar.WithTitle(options.Title);
            scalar.WithOpenApiRoutePattern("/" + prefix + "/{documentName}.json");
        });

        return app;
    }
}

using Snow.AspNetCore;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// 端点映射入口: 对应 ABP 的 <c>UseConfiguredEndpoints()</c>
/// <para>各模块在服务注册阶段把映射动作交进 <c>SnowEndpointRouterOptions</c>, 宿主在管道最后一步调本方法统一执行。</para>
/// </summary>
public static class SnowAspNetCoreApplicationBuilderExtensions
{
    /// <summary>
    /// 映射各模块登记的端点: 放在管道最后一步(所有中间件之后)
    /// </summary>
    /// <param name="app">应用构建器</param>
    /// <returns>应用构建器本身</returns>
    public static IApplicationBuilder UseSnowEndpoints(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        SnowAspNetCoreModule.MapEndpoints(app, app.ApplicationServices);
        return app;
    }
}

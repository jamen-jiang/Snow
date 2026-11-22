using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Snow.AspNetCore.Mvc;

/// <summary>
/// 自动 API 的端点挂载
/// </summary>
public static class SnowMvcApplicationBuilderExtensions
{
    /// <summary>
    /// 把自动 API 与手写 Controller 的路由一起挂上
    /// <para>必须在 <c>app.InitializeSnowAsync()</c> 之后调用, 因为它依赖模块已完成初始化。</para>
    /// </summary>
    /// <param name="app">应用构建器</param>
    /// <returns>应用构建器本身</returns>
    public static IApplicationBuilder UseSnowControllers(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        if (app is not IEndpointRouteBuilder endpoints)
        {
            throw new Snow.Modularity.SnowInitializationException(
                "当前应用不支持端点路由, 无法挂载自动 API: 请使用 WebApplication 作为宿主。");
        }

        endpoints.MapControllers();
        return app;
    }
}

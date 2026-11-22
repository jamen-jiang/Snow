using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Snow.AspNetCore.MultiTenancy;

namespace Snow.AspNetCore.MultiTenancy;

/// <summary>
/// 多租户的管道接线
/// </summary>
public static class SnowMultiTenancyApplicationBuilderExtensions
{
    /// <summary>
    /// 挂载多租户中间件
    /// <para>位置要在 <c>UseRouting()</c> 之后(路由出资者要能读到路由值)、认证授权与端点映射之前。</para>
    /// </summary>
    /// <param name="app">应用构建器</param>
    /// <returns>应用构建器本身, 便于链式调用</returns>
    public static IApplicationBuilder UseSnowMultiTenancy(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.UseSnowMiddlewareOnce<MultiTenancyMiddleware>();
    }
}
using Microsoft.Extensions.DependencyInjection;
using Snow.AspNetCore.MultiTenancy;
using Snow.MultiTenancy;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// 多租户中间件挂载(对应 ABP 的 <c>AbpAspNetCoreMultiTenancyApplicationBuilderExtensions</c>)
/// </summary>
public static class SnowMultiTenancyApplicationBuilderExtensions
{
    /// <summary>
    /// 挂上多租户解析: 必须在 <c>UseAuthentication()</c> 之后 —— 路由出资者要读路由值, 当前用户出资者要读已认证用户
    /// <para>认证之前挂会让"按用户解析租户"永远解析不到, 静默回落到宿主租户(不报错, 但数据写错租户)。</para>
    /// </summary>
    /// <param name="app">应用构建器</param>
    /// <returns>应用构建器本身</returns>
    public static IApplicationBuilder UseSnowMultiTenancy(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.UseMiddleware<MultiTenancyMiddleware>();
    }
}

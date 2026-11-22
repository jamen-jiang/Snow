using Microsoft.AspNetCore.Builder;
using Snow.AspNetCore.MultiTenancy;

namespace Snow.AspNetCore.MultiTenancy;

/// <summary>
/// 多租户的管道接线
/// </summary>
public static class SnowMultiTenancyApplicationBuilderExtensions
{
    /// <summary>
    /// 挂载多租户中间件
    /// <para>正常情况不用调: <c>SnowAspNetCoreMultiTenancyModule</c> 在应用初始化阶段已经自己挂好,
    /// 并顺带确保 <c>UseRouting()</c> 在管道里(路由出资者要能读到路由值)。</para>
    /// <para>这里保留是为了老宿主不改代码: 已挂过则直接返回, 不会重复执行。</para>
    /// </summary>
    /// <param name="app">应用构建器</param>
    /// <returns>应用构建器本身, 便于链式调用</returns>
    public static IApplicationBuilder UseSnowMultiTenancy(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.UseSnowMiddleware<MultiTenancyMiddleware>();
    }
}
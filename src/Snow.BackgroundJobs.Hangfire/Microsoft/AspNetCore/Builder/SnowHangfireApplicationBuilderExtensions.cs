using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using global::Hangfire;

namespace Snow.BackgroundJobs.Hangfire;

/// <summary>
/// 仪表盘中间件: 把 Hangfire 面板挂到管道上
/// <para>由 <see cref="SnowHangfireModule"/> 在应用初始化收尾阶段自动调用, 落点是授权之后、端点之前 ——
/// 面板是终端分支(命中路径自己写完响应), 早于授权挂就对匿名请求可见。</para>
/// </summary>
public static class SnowHangfireApplicationBuilderExtensions
{
    /// <summary>
    /// 挂上 Hangfire 仪表盘
    /// </summary>
    /// <param name="app">应用构建器</param>
    /// <param name="configure">可选: 追加仪表盘配置(标题、授权过滤器等), 在默认值之后执行</param>
    /// <returns>同一个应用构建器, 便于链式调用</returns>
    public static IApplicationBuilder UseSnowHangfireDashboard(this IApplicationBuilder app, Action<DashboardOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(app);

        SnowHangfireOptions options = app.ApplicationServices.GetRequiredService<IOptions<SnowHangfireOptions>>().Value;

        if (!options.EnableDashboard)
        {
            return app;
        }

        DashboardOptions dashboardOptions = new() { DashboardTitle = options.DashboardTitle };
        configure?.Invoke(dashboardOptions);

        return app.UseHangfireDashboard(options.DashboardPath, dashboardOptions);
    }
}

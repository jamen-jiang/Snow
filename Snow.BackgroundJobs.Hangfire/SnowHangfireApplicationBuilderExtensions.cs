using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using global::Hangfire;

namespace Snow.BackgroundJobs.Hangfire;

/// <summary>
/// 管道扩展: 挂载 Hangfire 仪表盘
/// </summary>
public static class SnowHangfireApplicationBuilderExtensions
{
    /// <summary>
    /// 挂载 Hangfire 仪表盘, 路径与标题取自 <see cref="SnowHangfireOptions"/>
    /// <para>正常情况不用调: <c>SnowHangfireModule</c> 已按配置自动挂好; 保留给需要自定义授权过滤器的宿主。</para>
    /// </summary>
    /// <param name="app">应用构建器</param>
    /// <param name="configure">追加/覆盖仪表盘选项(如授权过滤器)</param>
    /// <returns>同一个应用构建器, 便于链式调用</returns>
    public static IApplicationBuilder UseSnowHangfireDashboard(this IApplicationBuilder app, Action<DashboardOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(app);

        SnowHangfireOptions options = app.ApplicationServices.GetRequiredService<IOptions<SnowHangfireOptions>>().Value;
        if (!options.EnableDashboard)
        {
            return app;
        }

        DashboardOptions dashboardOptions = new()
        {
            DashboardTitle = options.DashboardTitle
        };

        if (!string.IsNullOrWhiteSpace(options.AppPath))
        {
            dashboardOptions.AppPath = options.AppPath;
        }

        configure?.Invoke(dashboardOptions);

        return app.UseHangfireDashboard(options.DashboardPath, dashboardOptions);
    }
}
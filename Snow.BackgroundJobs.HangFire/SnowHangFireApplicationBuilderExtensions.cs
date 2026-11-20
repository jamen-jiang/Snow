using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using global::Hangfire;

namespace Snow.BackgroundJobs.HangFire;

/// <summary>
/// 管道扩展: 挂载 Hangfire 仪表盘
/// </summary>
public static class SnowHangFireApplicationBuilderExtensions
{
    /// <summary>
    /// 挂载 Hangfire 仪表盘, 路径与标题取自 <see cref="SnowHangFireOptions"/>
    /// </summary>
    /// <param name="app">应用构建器</param>
    /// <param name="configure">追加/覆盖仪表盘选项(如授权过滤器)</param>
    /// <returns>同一个应用构建器, 便于链式调用</returns>
    public static IApplicationBuilder UseSnowHangFireDashboard(this IApplicationBuilder app, Action<DashboardOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(app);

        SnowHangFireOptions options = app.ApplicationServices.GetRequiredService<IOptions<SnowHangFireOptions>>().Value;
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

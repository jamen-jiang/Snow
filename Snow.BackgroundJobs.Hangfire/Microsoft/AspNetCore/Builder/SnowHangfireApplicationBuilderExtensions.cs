using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Snow.BackgroundJobs.Hangfire;
using global::Hangfire;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Hangfire 仪表盘挂载
/// </summary>
public static class SnowHangfireApplicationBuilderExtensions
{
    private const string Marker = "Snow.Middleware.HangfireDashboard";

    /// <summary>
    /// 挂上 Hangfire 仪表盘, 路径与标题取自 <see cref="SnowHangfireOptions"/>
    /// <para>它是终端分支(自己不调 next), 而且必须在认证与授权之后挂 —— 否则面板对匿名请求可见。
    /// <c>EnableDashboard</c> 为 false 时直接返回。</para>
    /// <para>重复调用不会重复挂。</para>
    /// </summary>
    /// <param name="app">应用构建器</param>
    /// <param name="configure">追加/覆盖仪表盘选项(如授权过滤器)</param>
    /// <returns>同一个应用构建器, 便于链式调用</returns>
    public static IApplicationBuilder UseSnowHangfireDashboard(this IApplicationBuilder app, Action<DashboardOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(app);

        if (app.Properties.ContainsKey(Marker))
        {
            return app;
        }

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

        app.Properties[Marker] = true;
        return app.UseHangfireDashboard(options.DashboardPath, dashboardOptions);
    }
}

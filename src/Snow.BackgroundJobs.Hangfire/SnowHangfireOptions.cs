using global::Hangfire;
using global::Hangfire.Dashboard;

namespace Snow.BackgroundJobs.Hangfire;

/// <summary>
/// Hangfire 集成选项
/// <para>模块不读配置节(与 ABP 的 <c>AbpHangfireOptions</c> 一致): 宿主 <c>Configure&lt;SnowHangfireOptions&gt;(...)</c> 给出, 不配就用默认值。</para>
/// <para>不复刻 Hangfire 的字段: 服务端参数放 <see cref="ServerOptions"/>, 仪表盘参数放 <see cref="DashboardOptions"/>,
/// Hangfire 有的就直接改那个对象。</para>
/// <para>只管服务端; 存储由宿主在自己的模块 <c>ConfigureServices</c> 里用 Hangfire 的 <c>AddHangfire(...)</c> 追加,
/// 仪表盘中间件是宿主的事(<c>app.UseSnowHangfireDashboard()</c>), 本包不碰 Web。</para>
/// </summary>
public class SnowHangfireOptions
{
    /// <summary>
    /// 是否创建 Hangfire 服务端(后台作业执行器), 默认 true
    /// <para>只入队不执行的实例设成 false, 容器里解析 <see cref="BackgroundJobServer"/> 会得到 null。</para>
    /// </summary>
    public bool EnableServer { get; set; } = true;

    /// <summary>
    /// 服务端参数, 空则用 Hangfire 的默认值
    /// <para>工作线程数、轮询间隔、队列、服务名、关闭超时等都在这个对象里。</para>
    /// </summary>
    public BackgroundJobServerOptions? ServerOptions { get; set; }

    /// <summary>
    /// 仪表盘参数, 空则用 Hangfire 的默认值
    /// <para>标题、授权过滤器、统计轮询间隔等都在这个对象里。</para>
    /// </summary>
    public DashboardOptions? DashboardOptions { get; set; }

    /// <summary>
    /// 仪表盘路径, 默认 <c>/hangfire</c>
    /// </summary>
    public string DashboardPath { get; set; } = "/hangfire";

    /// <summary>
    /// 周期任务的时区 Id, 空则按 UTC
    /// </summary>
    public string? TimeZoneId { get; set; }
}

namespace Snow.BackgroundJobs.HangFire;

/// <summary>
/// Hangfire 集成配置, 对应配置节 <c>HangFire</c>
/// </summary>
public class SnowHangFireOptions
{
    /// <summary>
    /// 是否注册 Hangfire 服务端(后台作业执行器), 默认 true
    /// </summary>
    public bool EnableServer { get; set; } = true;

    /// <summary>
    /// 是否启用仪表盘, 默认 true
    /// </summary>
    public bool EnableDashboard { get; set; } = true;

    /// <summary>
    /// 仪表盘路径, 默认 /hangfire
    /// </summary>
    public string DashboardPath { get; set; } = "/hangfire";

    /// <summary>
    /// 仪表盘标题
    /// </summary>
    public string DashboardTitle { get; set; } = "Snow 后台任务";

    /// <summary>
    /// 仪表盘返回站点根路径(部署在子路径时设置)
    /// </summary>
    public string? AppPath { get; set; }

    /// <summary>
    /// 宿主未自行配置存储时是否退回进程内存储, 默认 true
    /// <para>生产环境应显式调用 AddHangfire 配置持久化存储, 本开关随之失效</para>
    /// </summary>
    public bool UseInMemoryStorage { get; set; } = true;

    /// <summary>
    /// 服务端工作线程数, 空则取处理器核数
    /// </summary>
    public int? WorkerCount { get; set; }

    /// <summary>
    /// 服务端轮询间隔(秒), 默认 1
    /// <para>使用数据库存储且任务量很大时可调大到 15。</para>
    /// </summary>
    public int PollingIntervalSeconds { get; set; } = 1;

    /// <summary>
    /// 周期任务的时区 Id, 空则按 UTC
    /// </summary>
    public string? TimeZoneId { get; set; }
}

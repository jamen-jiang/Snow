namespace Snow.BackgroundJobs.Hangfire;

/// <summary>
/// Hangfire 集成配置, 对应配置节 <c>Hangfire</c>
/// <para>只管服务端与存储; 仪表盘是宿主的事(<c>app.UseHangfireDashboard(...)</c>), 本包不碰 Web。</para>
/// </summary>
public class SnowHangfireOptions
{
    /// <summary>
    /// 是否注册 Hangfire 服务端(后台作业执行器), 默认 true
    /// </summary>
    public bool EnableServer { get; set; } = true;

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
    /// 是否在应用初始化收尾阶段挂 Hangfire 仪表盘, 默认 true
    /// <para>关掉后 <c>UseSnowHangfireDashboard</c> 直接返回, 宿主可以自己决定挂不挂、挂在哪种管道上</para>
    /// </summary>
    public bool EnableDashboard { get; set; } = true;

    /// <summary>
    /// 仪表盘路径, 默认 <c>/hangfire</c>
    /// </summary>
    public string DashboardPath { get; set; } = "/hangfire";

    /// <summary>
    /// 仪表盘标题, 默认 <c>Snow 后台任务</c>
    /// </summary>
    public string DashboardTitle { get; set; } = "Snow 后台任务";

    /// <summary>
    /// 周期任务的时区 Id, 空则按 UTC
    /// </summary>
    public string? TimeZoneId { get; set; }
}

namespace Snow.BackgroundJobs;

/// <summary>
/// 任务 worker 配置: 轮询间隔、批量大小、失败退避与分布式锁
/// </summary>
public class BackgroundJobWorkerOptions
{
    /// <summary>
    /// 轮询间隔, 默认 5 秒
    /// </summary>
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// 单轮最多取多少条任务, 默认 100
    /// </summary>
    public int MaxJobFetchCount { get; set; } = 100;

    /// <summary>
    /// 首次失败后的等待时长, 默认 60 秒
    /// </summary>
    public TimeSpan FirstWaitDuration { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// 退避倍数, 默认 2.0(每次失败后等待时长乘以该值)
    /// </summary>
    public double WaitFactor { get; set; } = 2.0;

    /// <summary>
    /// 任务从创建到放弃的最长时长, 默认 2 天
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromDays(2);

    /// <summary>
    /// 是否启用分布式锁(多实例部署时避免重复消费), 默认 true
    /// </summary>
    public bool UseDistributedLock { get; set; } = true;

    /// <summary>
    /// worker 分布式锁名
    /// </summary>
    public string DistributedLockName { get; set; } = "SnowBackgroundJobWorker";
}

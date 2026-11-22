using System;

namespace Snow.BackgroundJobs;

/// <summary>
/// 后台任务配置, 对应配置节 <c>BackgroundJobs</c>
/// <para>执行开关与轮询参数都在这里: 只有一套开关, 不会出现"到底哪个才是关"的疑问。</para>
/// </summary>
public class BackgroundJobOptions
{
    /// <summary>
    /// 是否启用内置 worker, 默认 true
    /// <para>换成 Hangfire 等外部调度器时由那个模块自动置 false。</para>
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 轮询间隔, 默认 5 秒
    /// </summary>
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// 单轮最多取多少条任务, 默认 100
    /// </summary>
    public int MaxJobFetchCount { get; set; } = 100;

    /// <summary>
    /// 首次失败后等多久重试, 默认 60 秒; 之后按 <see cref="WaitFactor"/> 递增
    /// </summary>
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// 重试间隔的递增倍数, 默认 2
    /// </summary>
    public double WaitFactor { get; set; } = 2.0;

    /// <summary>
    /// 最多尝试几次(含第一次), 超过即放弃, 默认 5
    /// </summary>
    public int MaxRetryCount { get; set; } = 5;

    /// <summary>
    /// 是否启用分布式锁(多实例部署时避免重复消费), 默认 true
    /// </summary>
    public bool UseDistributedLock { get; set; } = true;

    /// <summary>
    /// worker 分布式锁名
    /// </summary>
    public string DistributedLockName { get; set; } = "SnowBackgroundJobWorker";

    /// <summary>
    /// 抢不到分布式锁时等多久再抢, 默认 60 秒
    /// <para>别的实例正在消费时立即重抢只会空转, 退避一轮更划算。</para>
    /// </summary>
    public TimeSpan LockUnavailableDelay { get; set; } = TimeSpan.FromSeconds(60);
}

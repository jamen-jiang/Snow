using System.Linq.Expressions;
using Microsoft.Extensions.Options;
using Snow.BackgroundJobs;
using global::Hangfire;

namespace Snow.BackgroundJobs.HangFire;

/// <summary>
/// Hangfire 版调度: 表达式直接交给 Hangfire, 不经过任何"名字 → 类型"的运行时查找
/// </summary>
public sealed class HangFireBackgroundJobScheduler : IBackgroundJobScheduler
{
    private readonly IRecurringJobManager _recurringJobManager;

    private readonly IBackgroundJobClient _backgroundJobClient;

    private readonly IOptions<SnowHangFireOptions> _options;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="recurringJobManager">周期任务管理器</param>
    /// <param name="backgroundJobClient">即时任务客户端</param>
    /// <param name="options">Hangfire 集成配置</param>
    public HangFireBackgroundJobScheduler(IRecurringJobManager recurringJobManager, IBackgroundJobClient backgroundJobClient, IOptions<SnowHangFireOptions> options)
    {
        _recurringJobManager = recurringJobManager;
        _backgroundJobClient = backgroundJobClient;
        _options = options;
    }

    /// <inheritdoc />
    public void ScheduleRecurring<TJob>(string id, string cron, Expression<Func<TJob, Task>> job)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(cron);
        ArgumentNullException.ThrowIfNull(job);

        _recurringJobManager.AddOrUpdate(id, job, cron, new RecurringJobOptions { TimeZone = ResolveTimeZone(_options.Value.TimeZoneId) });
    }

    /// <inheritdoc />
    public void DeleteRecurring(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        _recurringJobManager.RemoveIfExists(id);
    }

    /// <inheritdoc />
    public void TriggerOnce<TJob>(Expression<Func<TJob, Task>> job)
    {
        ArgumentNullException.ThrowIfNull(job);

        _backgroundJobClient.Enqueue(job);
    }

    private static TimeZoneInfo ResolveTimeZone(string? timeZoneId)
    {
        return string.IsNullOrWhiteSpace(timeZoneId) ? TimeZoneInfo.Utc : TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
    }
}

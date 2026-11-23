using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using global::Hangfire;
using global::Hangfire.Common;
using global::Hangfire.States;
using Snow.DependencyInjection;

namespace Snow.BackgroundJobs.Hangfire;

/// <summary>
/// Hangfire 调度实现: 周期任务与一次性任务都直接传 lambda
/// </summary>
[SingletonService(ReplaceServices = true)]
public sealed class HangfireBackgroundJobScheduler : IBackgroundJobScheduler
{
    private readonly IRecurringJobManager _recurringJobManager;

    private readonly IBackgroundJobClient _backgroundJobClient;

    private readonly TimeZoneInfo _timeZone;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="recurringJobManager">周期任务管理器</param>
    /// <param name="backgroundJobClient">入队客户端</param>
    /// <param name="options">Hangfire 配置</param>
    public HangfireBackgroundJobScheduler(
        IRecurringJobManager recurringJobManager,
        IBackgroundJobClient backgroundJobClient,
        IOptions<SnowHangfireOptions> options)
    {
        _recurringJobManager = recurringJobManager;
        _backgroundJobClient = backgroundJobClient;
        _timeZone = ResolveTimeZone(options.Value.TimeZoneId);
    }

    /// <inheritdoc />
    public void ScheduleRecurring<TService>(string id, string cron, Expression<Action<TService>> job)
    {
        AddOrUpdate(id, cron, Job.FromExpression(job));
    }

    /// <inheritdoc />
    public void ScheduleRecurring<TService>(string id, string cron, Expression<Func<TService, Task>> job)
    {
        AddOrUpdate(id, cron, Job.FromExpression(job));
    }

    /// <inheritdoc />
    public void DeleteRecurring(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        _recurringJobManager.RemoveIfExists(id);
    }

    /// <inheritdoc />
    public void TriggerOnce<TService>(Expression<Action<TService>> job)
    {
        _backgroundJobClient.Create(Job.FromExpression(job), new EnqueuedState());
    }

    /// <inheritdoc />
    public void TriggerOnce<TService>(Expression<Func<TService, Task>> job)
    {
        _backgroundJobClient.Create(Job.FromExpression(job), new EnqueuedState());
    }

    private void AddOrUpdate(string id, string cron, Job job)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(cron);

        _recurringJobManager.AddOrUpdate(id, job, cron, new RecurringJobOptions { TimeZone = _timeZone });
    }

    private static TimeZoneInfo ResolveTimeZone(string? timeZoneId)
    {
        return string.IsNullOrWhiteSpace(timeZoneId) ? TimeZoneInfo.Utc : TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
    }
}
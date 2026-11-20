using Microsoft.Extensions.Options;
using Snow.BackgroundJobs;
using global::Hangfire;

namespace Snow.BackgroundJobs.HangFire;

/// <summary>
/// <see cref="IBackgroundJobScheduler"/> 的 Hangfire 实现
/// </summary>
public class HangFireBackgroundJob : IBackgroundJobScheduler
{
    private readonly IRecurringJobManager _recurringJobManager;

    private readonly IBackgroundJobClient _backgroundJobClient;

    private readonly IOptions<SnowHangFireOptions> _options;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="recurringJobManager">周期任务管理器</param>
    /// <param name="backgroundJobClient">即时任务客户端</param>
    /// <param name="options">集成配置</param>
    public HangFireBackgroundJob(IRecurringJobManager recurringJobManager, IBackgroundJobClient backgroundJobClient, IOptions<SnowHangFireOptions> options)
    {
        _recurringJobManager = recurringJobManager;
        _backgroundJobClient = backgroundJobClient;
        _options = options;
    }

    /// <inheritdoc />
    public void ScheduleRecurring(string id, string cron, string beanName, string methodName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(cron);
        ArgumentException.ThrowIfNullOrWhiteSpace(beanName);
        ArgumentException.ThrowIfNullOrWhiteSpace(methodName);

        _recurringJobManager.AddOrUpdate<HangFireJobInvoker>(
            id,
            invoker => invoker.InvokeAsync(beanName, methodName),
            cron,
            CreateRecurringJobOptions());
    }

    /// <inheritdoc />
    public void DeleteRecurring(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        _recurringJobManager.RemoveIfExists(id);
    }

    /// <inheritdoc />
    public void TriggerOnce(string beanName, string methodName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(beanName);
        ArgumentException.ThrowIfNullOrWhiteSpace(methodName);

        _backgroundJobClient.Enqueue<HangFireJobInvoker>(invoker => invoker.InvokeAsync(beanName, methodName));
    }

    private RecurringJobOptions CreateRecurringJobOptions()
    {
        return new RecurringJobOptions
        {
            TimeZone = ResolveTimeZone(_options.Value.TimeZoneId)
        };
    }

    private static TimeZoneInfo ResolveTimeZone(string? timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return TimeZoneInfo.Utc;
        }

        return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
    }
}

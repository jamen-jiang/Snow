using Microsoft.Extensions.Options;
using Snow.BackgroundJobs;
using global::Hangfire;

namespace Snow.BackgroundJobs.HangFire;

/// <summary>
/// <see cref="IBackgroundJobManager"/> 的 Hangfire 实现: 入队即投递到 Hangfire, 由 Hangfire 服务端执行与重试
/// <para>不写 <see cref="IBackgroundJobStore"/>: 任务持久化、重试与看板都交给 Hangfire 自己管。</para>
/// </summary>
public sealed class HangFireBackgroundJobManager : IBackgroundJobManager
{
    private readonly IBackgroundJobClient _backgroundJobClient;

    private readonly IBackgroundJobSerializer _serializer;

    private readonly IOptions<BackgroundJobOptions> _options;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="backgroundJobClient">即时任务客户端</param>
    /// <param name="serializer">参数序列化</param>
    /// <param name="options">任务配置</param>
    public HangFireBackgroundJobManager(IBackgroundJobClient backgroundJobClient, IBackgroundJobSerializer serializer, IOptions<BackgroundJobOptions> options)
    {
        _backgroundJobClient = backgroundJobClient;
        _serializer = serializer;
        _options = options;
    }

    /// <inheritdoc />
    public Task<string> EnqueueAsync<TArgs>(TArgs args, BackgroundJobPriority priority = BackgroundJobPriority.Normal, TimeSpan? delay = null, CancellationToken cancellationToken = default)
    {
        return EnqueueAsync(typeof(TArgs), args, priority, delay, cancellationToken);
    }

    /// <inheritdoc />
    public Task<string> EnqueueAsync(Type argsType, object? args, BackgroundJobPriority priority = BackgroundJobPriority.Normal, TimeSpan? delay = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(argsType);

        BackgroundJobOptions options = _options.Value;

        // 未登记就失败, 避免任务投递后 Hangfire 端永远找不到实现
        options.GetJobType(argsType);

        string jobName = options.GetJobName(argsType);
        string jobArgs = _serializer.Serialize(args, argsType);

        // 开源版 Hangfire 没有优先级概念, priority 只在换回内置队列引擎时才有意义
        string jobId = delay.HasValue
            ? _backgroundJobClient.Schedule<HangFireBackgroundJobRunner>(runner => runner.RunAsync(jobName, jobArgs), delay.Value)
            : _backgroundJobClient.Enqueue<HangFireBackgroundJobRunner>(runner => runner.RunAsync(jobName, jobArgs));

        return Task.FromResult(jobId);
    }
}

using Microsoft.Extensions.Options;
using Snow.BackgroundJobs;
using global::Hangfire;

namespace Snow.BackgroundJobs.HangFire;

/// <summary>
/// Hangfire 版入队: 直接投给 Hangfire, 由它持久化、执行与重试
/// <para>不写 <see cref="IBackgroundJobStore"/>: 任务状态与看板都交给 Hangfire 自己管。</para>
/// </summary>
public sealed class HangFireBackgroundJobManager : IBackgroundJobManager
{
    private readonly IBackgroundJobClient _backgroundJobClient;

    private readonly IOptions<BackgroundJobOptions> _options;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="backgroundJobClient">即时任务客户端</param>
    /// <param name="options">任务配置</param>
    public HangFireBackgroundJobManager(IBackgroundJobClient backgroundJobClient, IOptions<BackgroundJobOptions> options)
    {
        _backgroundJobClient = backgroundJobClient;
        _options = options;
    }

    /// <inheritdoc />
    public Task<string> EnqueueAsync<TArgs>(TArgs args, TimeSpan? delay = null, CancellationToken cancellationToken = default)
    {
        BackgroundJobOptions options = _options.Value;

        // 未登记就失败, 避免任务投递后 Hangfire 端永远找不到实现
        options.GetJobType(typeof(TArgs));

        string jobName = options.GetJobName(typeof(TArgs));
        string jobArgs = BackgroundJobSerializer.Serialize(args, typeof(TArgs));

        string jobId = delay.HasValue
            ? _backgroundJobClient.Schedule<HangFireJobInvoker>(invoker => invoker.RunAsync(jobName, jobArgs), delay.Value)
            : _backgroundJobClient.Enqueue<HangFireJobInvoker>(invoker => invoker.RunAsync(jobName, jobArgs));

        return Task.FromResult(jobId);
    }
}

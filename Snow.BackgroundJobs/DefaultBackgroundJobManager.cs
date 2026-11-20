using Microsoft.Extensions.Options;
using Snow.DependencyInjection;

namespace Snow.BackgroundJobs;

/// <summary>
/// 默认任务管理器: 校验任务已登记后写一条记录到存储, 由 worker 轮询执行
/// </summary>
public sealed class DefaultBackgroundJobManager : IBackgroundJobManager, ITransientDependency
{
    private readonly IBackgroundJobStore _store;

    private readonly IBackgroundJobSerializer _serializer;

    private readonly IOptions<BackgroundJobOptions> _options;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="store">任务存储</param>
    /// <param name="serializer">参数序列化</param>
    /// <param name="options">任务配置</param>
    public DefaultBackgroundJobManager(IBackgroundJobStore store, IBackgroundJobSerializer serializer, IOptions<BackgroundJobOptions> options)
    {
        _store = store;
        _serializer = serializer;
        _options = options;
    }

    /// <inheritdoc />
    public Task<string> EnqueueAsync<TArgs>(TArgs args, BackgroundJobPriority priority = BackgroundJobPriority.Normal, TimeSpan? delay = null, CancellationToken cancellationToken = default)
    {
        return EnqueueAsync(typeof(TArgs), args, priority, delay, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<string> EnqueueAsync(Type argsType, object? args, BackgroundJobPriority priority = BackgroundJobPriority.Normal, TimeSpan? delay = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(argsType);

        BackgroundJobOptions options = _options.Value;

        // 未登记就让入队失败, 避免任务进库后永远等不到实现
        options.GetJobType(argsType);

        DateTime now = DateTime.UtcNow;
        BackgroundJobInfo jobInfo = new()
        {
            Id = Guid.NewGuid(),
            JobName = options.GetJobName(argsType),
            JobArgs = _serializer.Serialize(args, argsType),
            Priority = priority,
            CreationTime = now,
            NextTryTime = delay.HasValue ? now.Add(delay.Value) : now
        };

        await _store.InsertAsync(jobInfo, cancellationToken).ConfigureAwait(false);
        return jobInfo.Id.ToString();
    }
}

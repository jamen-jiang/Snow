using Microsoft.Extensions.Options;
using Snow.DependencyInjection;

namespace Snow.BackgroundJobs;

/// <summary>
/// 默认入口实现: 参数序列化后写进 <see cref="IBackgroundJobStore"/>, 由内置 worker 消费
/// </summary>
public sealed class DefaultBackgroundJobManager : IBackgroundJobManager, ITransientDependency
{
    private readonly IBackgroundJobStore _store;

    private readonly IOptions<BackgroundJobOptions> _options;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="store">任务存储</param>
    /// <param name="options">任务配置</param>
    public DefaultBackgroundJobManager(IBackgroundJobStore store, IOptions<BackgroundJobOptions> options)
    {
        _store = store;
        _options = options;
    }

    /// <inheritdoc />
    public async Task<string> EnqueueAsync<TArgs>(TArgs args, TimeSpan? delay = null, CancellationToken cancellationToken = default)
    {
        BackgroundJobOptions options = _options.Value;

        // 未登记就失败: 任务一旦进库却没有实现, 只会等到超时才被放弃
        options.GetJobType(typeof(TArgs));

        DateTime now = DateTime.UtcNow;
        BackgroundJobInfo jobInfo = new()
        {
            Id = Guid.NewGuid(),
            JobName = options.GetJobName(typeof(TArgs)),
            JobArgs = BackgroundJobSerializer.Serialize(args, typeof(TArgs)),
            CreationTime = now,
            NextTryTime = delay.HasValue ? now.Add(delay.Value) : now
        };

        await _store.InsertAsync(jobInfo, cancellationToken).ConfigureAwait(false);
        return jobInfo.Id.ToString();
    }
}

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Snow.DependencyInjection;
using Snow.Timing;

namespace Snow.BackgroundJobs;

/// <summary>
/// 进程内任务存储: 单实例够用, 多实例共享请换成数据库/Redis 实现
/// </summary>
[SingletonService]
public sealed class InMemoryBackgroundJobStore : IBackgroundJobStore
{
    private readonly ConcurrentDictionary<Guid, BackgroundJobInfo> _jobs = [];

    private readonly IClock _clock;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="clock">时间提供器</param>
    public InMemoryBackgroundJobStore(IClock clock)
    {
        _clock = clock;
    }

    /// <summary>
    /// 当前积压任务数
    /// </summary>
    public int Count => _jobs.Count;

    /// <inheritdoc />
    public Task InsertAsync(BackgroundJobInfo jobInfo, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(jobInfo);

        _jobs[jobInfo.Id] = jobInfo.Clone();

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<List<BackgroundJobInfo>> GetWaitingJobsAsync(int maxResultCount, CancellationToken cancellationToken = default)
    {
        DateTime now = _clock.UtcNow;
        int take = maxResultCount <= 0 ? 100 : maxResultCount;

        List<BackgroundJobInfo> jobs = _jobs.Values
            .Where(job => job.NextTryTime <= now)
            .OrderBy(job => job.TryCount)
            .ThenBy(job => job.NextTryTime)
            .Take(take)
            .Select(job => job.Clone())
            .ToList();

        return Task.FromResult(jobs);
    }

    /// <inheritdoc />
    public Task UpdateAsync(BackgroundJobInfo jobInfo, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(jobInfo);

        _jobs[jobInfo.Id] = jobInfo.Clone();

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DeleteAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        _jobs.TryRemove(jobId, out _);

        return Task.CompletedTask;
    }
}

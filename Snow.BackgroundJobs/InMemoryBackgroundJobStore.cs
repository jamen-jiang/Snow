using System.Collections.Concurrent;

namespace Snow.BackgroundJobs;

/// <summary>
/// 进程内任务存储: 零依赖的默认实现, 单实例够用, 重启即丢
/// </summary>
public sealed class InMemoryBackgroundJobStore : IBackgroundJobStore
{
    private readonly ConcurrentDictionary<Guid, BackgroundJobInfo> _jobs = new();

    /// <summary>
    /// 当前任务总数(含已放弃)
    /// </summary>
    public int Count => _jobs.Count;

    /// <inheritdoc />
    public Task InsertAsync(BackgroundJobInfo jobInfo, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(jobInfo);

        _jobs[jobInfo.Id] = jobInfo;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<List<BackgroundJobInfo>> GetWaitingJobsAsync(int maxResultCount, CancellationToken cancellationToken = default)
    {
        DateTime now = DateTime.UtcNow;

        List<BackgroundJobInfo> jobs = _jobs.Values
            .Where(job => !job.IsAbandoned && job.NextTryTime <= now)
            .OrderByDescending(job => job.Priority)
            .ThenBy(job => job.TryCount)
            .ThenBy(job => job.NextTryTime)
            .Take(maxResultCount <= 0 ? 100 : maxResultCount)
            .ToList();

        return Task.FromResult(jobs);
    }

    /// <inheritdoc />
    public Task<BackgroundJobInfo?> FindAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_jobs.TryGetValue(jobId, out BackgroundJobInfo? job) ? job : null);
    }

    /// <inheritdoc />
    public Task UpdateAsync(BackgroundJobInfo jobInfo, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(jobInfo);

        if (jobInfo.IsAbandoned)
        {
            _jobs.TryRemove(jobInfo.Id, out _);
            return Task.CompletedTask;
        }

        _jobs[jobInfo.Id] = jobInfo;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DeleteAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        _jobs.TryRemove(jobId, out _);
        return Task.CompletedTask;
    }
}

namespace Snow.BackgroundJobs;

/// <summary>
/// 任务存储: 只负责持久化排队中的任务, 重试与放弃策略在 worker 里
/// <para>默认实现是进程内存储; 需要重启不丢任务时换成数据库实现即可。</para>
/// </summary>
public interface IBackgroundJobStore
{
    /// <summary>
    /// 写入一条任务
    /// </summary>
    /// <param name="jobInfo">任务</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>表示写入过程的任务</returns>
    Task InsertAsync(BackgroundJobInfo jobInfo, CancellationToken cancellationToken = default);

    /// <summary>
    /// 取出到点可执行的任务, 按尝试次数与下次执行时间升序
    /// </summary>
    /// <param name="maxResultCount">最多取多少条</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务列表</returns>
    Task<List<BackgroundJobInfo>> GetWaitingJobsAsync(int maxResultCount, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新一条任务(已放弃的任务允许直接移除)
    /// </summary>
    /// <param name="jobInfo">任务</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>表示更新过程的任务</returns>
    Task UpdateAsync(BackgroundJobInfo jobInfo, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除一条任务
    /// </summary>
    /// <param name="jobId">任务 Id</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>表示删除过程的任务</returns>
    Task DeleteAsync(Guid jobId, CancellationToken cancellationToken = default);
}

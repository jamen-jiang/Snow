namespace Snow.BackgroundJobs;

/// <summary>
/// 后台任务存储: 入队即落库, worker 轮询取出待执行的任务
/// <para>实现包提供的默认实现是进程内存储; 换成数据库/Redis 实现即可多实例共享。</para>
/// </summary>
public interface IBackgroundJobStore
{
    /// <summary>
    /// 插入一条任务
    /// </summary>
    /// <param name="jobInfo">任务记录</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>表示插入过程的任务</returns>
    Task InsertAsync(BackgroundJobInfo jobInfo, CancellationToken cancellationToken = default);

    /// <summary>
    /// 取待执行的任务: NextTryTime 已到、未放弃, 按优先级降序、尝试次数升序、下次执行时间升序
    /// </summary>
    /// <param name="maxResultCount">本次最多取多少条</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>待执行任务列表</returns>
    Task<List<BackgroundJobInfo>> GetWaitingJobsAsync(int maxResultCount, CancellationToken cancellationToken = default);

    /// <summary>
    /// 按 Id 取任务
    /// </summary>
    /// <param name="jobId">任务 Id</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务记录; 不存在时返回 null</returns>
    Task<BackgroundJobInfo?> FindAsync(Guid jobId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新任务(成功后删除, 失败后回写重试信息)
    /// </summary>
    /// <param name="jobInfo">任务记录</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>表示更新过程的任务</returns>
    Task UpdateAsync(BackgroundJobInfo jobInfo, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除任务
    /// </summary>
    /// <param name="jobId">任务 Id</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>表示删除过程的任务</returns>
    Task DeleteAsync(Guid jobId, CancellationToken cancellationToken = default);
}

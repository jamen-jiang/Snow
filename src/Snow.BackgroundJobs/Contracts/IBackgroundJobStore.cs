using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Snow.BackgroundJobs;

/// <summary>
/// 任务存储: 内置实现是进程内字典, 换数据库实现即可多实例共享
/// </summary>
public interface IBackgroundJobStore
{
    /// <summary>
    /// 写入任务
    /// </summary>
    /// <param name="jobInfo">任务记录</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task InsertAsync(BackgroundJobInfo jobInfo, CancellationToken cancellationToken = default);

    /// <summary>
    /// 取待执行任务: 到了执行时间, 先失败先重试
    /// </summary>
    /// <param name="maxResultCount">最多取多少条</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务列表(副本)</returns>
    Task<List<BackgroundJobInfo>> GetWaitingJobsAsync(int maxResultCount, CancellationToken cancellationToken = default);

    /// <summary>
    /// 回写任务
    /// </summary>
    /// <param name="jobInfo">任务记录</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task UpdateAsync(BackgroundJobInfo jobInfo, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除任务
    /// </summary>
    /// <param name="jobId">任务主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task DeleteAsync(Guid jobId, CancellationToken cancellationToken = default);
}

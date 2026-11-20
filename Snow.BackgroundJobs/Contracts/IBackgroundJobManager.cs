namespace Snow.BackgroundJobs;

/// <summary>
/// 后台任务入口: 只有"入队"一件事 —— 参数类型决定执行哪个作业
/// <para>周期任务与一次性触发是另一件事, 见 <see cref="IBackgroundJobScheduler"/>; 两者互不牵连。</para>
/// </summary>
public interface IBackgroundJobManager
{
    /// <summary>
    /// 入队一个任务
    /// </summary>
    /// <typeparam name="TArgs">任务参数类型, 决定执行哪个 <see cref="IBackgroundJob{TArgs}"/> 实现</typeparam>
    /// <param name="args">任务参数</param>
    /// <param name="delay">延迟多久后最早可执行, 空表示立刻</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务 Id</returns>
    Task<string> EnqueueAsync<TArgs>(TArgs args, TimeSpan? delay = null, CancellationToken cancellationToken = default);
}

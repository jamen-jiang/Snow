namespace Snow.BackgroundJobs;

/// <summary>
/// 后台任务管理器: 业务代码只调它入队, 不关心任务最后由谁执行
/// </summary>
public interface IBackgroundJobManager
{
    /// <summary>
    /// 入队一个任务
    /// </summary>
    /// <typeparam name="TArgs">任务参数类型</typeparam>
    /// <param name="args">任务参数</param>
    /// <param name="priority">优先级</param>
    /// <param name="delay">延迟多久后最早可执行, 空表示立刻</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务 Id</returns>
    Task<string> EnqueueAsync<TArgs>(TArgs args, BackgroundJobPriority priority = BackgroundJobPriority.Normal, TimeSpan? delay = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 按参数类型入队(参数类型在运行期才知道时使用)
    /// </summary>
    /// <param name="argsType">任务参数类型</param>
    /// <param name="args">任务参数</param>
    /// <param name="priority">优先级</param>
    /// <param name="delay">延迟多久后最早可执行, 空表示立刻</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务 Id</returns>
    Task<string> EnqueueAsync(Type argsType, object? args, BackgroundJobPriority priority = BackgroundJobPriority.Normal, TimeSpan? delay = null, CancellationToken cancellationToken = default);
}

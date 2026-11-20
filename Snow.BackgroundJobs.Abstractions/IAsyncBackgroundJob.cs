namespace Snow.BackgroundJobs;

/// <summary>
/// 异步后台任务: 与 <see cref="IBackgroundJob{TArgs}"/> 同构, 执行体为 <see cref="ExecuteAsync"/>
/// </summary>
/// <typeparam name="TArgs">任务参数类型</typeparam>
public interface IAsyncBackgroundJob<in TArgs>
{
    /// <summary>
    /// 执行任务
    /// </summary>
    /// <param name="args">任务参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>表示执行过程的任务</returns>
    Task ExecuteAsync(TArgs args, CancellationToken cancellationToken = default);
}

namespace Snow.BackgroundJobs;

/// <summary>
/// 后台任务: 一个参数类型对应一个任务实现, 由 <see cref="IBackgroundJobManager"/> 入队后异步执行。
/// <para>同步版本; 需要异步时实现 <see cref="IAsyncBackgroundJob{TArgs}"/> 或继承 <see cref="AsyncBackgroundJob{TArgs}"/>。</para>
/// </summary>
/// <typeparam name="TArgs">任务参数类型</typeparam>
public interface IBackgroundJob<in TArgs>
{
    /// <summary>
    /// 执行任务
    /// </summary>
    /// <param name="args">任务参数</param>
    void Execute(TArgs args);
}

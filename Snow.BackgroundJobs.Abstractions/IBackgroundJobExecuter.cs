namespace Snow.BackgroundJobs;

/// <summary>
/// 任务执行器: 把一段执行上下文落到具体的作业类实例上
/// </summary>
public interface IBackgroundJobExecuter
{
    /// <summary>
    /// 执行任务
    /// </summary>
    /// <param name="context">执行上下文</param>
    /// <returns>表示执行过程的任务</returns>
    Task ExecuteAsync(JobExecutionContext context);
}

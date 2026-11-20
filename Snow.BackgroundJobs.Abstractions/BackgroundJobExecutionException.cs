namespace Snow.BackgroundJobs;

/// <summary>
/// 后台任务执行失败: 包装业务异常, 并带上任务类型与参数便于排查
/// </summary>
public class BackgroundJobExecutionException : Exception
{
    /// <summary>
    /// 任务类型(程序集限定名)
    /// </summary>
    public string JobType { get; set; } = default!;

    /// <summary>
    /// 任务参数
    /// </summary>
    public object? JobArgs { get; set; }

    /// <summary>
    /// 构造器
    /// </summary>
    public BackgroundJobExecutionException() { }

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="message">异常消息</param>
    /// <param name="innerException">内部异常</param>
    public BackgroundJobExecutionException(string message, Exception innerException) : base(message, innerException) { }
}

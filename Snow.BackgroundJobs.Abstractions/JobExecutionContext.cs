namespace Snow.BackgroundJobs;

/// <summary>
/// 任务执行上下文: 交给 <see cref="IBackgroundJobExecuter"/> 的一段执行信息
/// </summary>
public sealed class JobExecutionContext
{
    /// <summary>
    /// 当前作用域的服务提供者
    /// </summary>
    public IServiceProvider ServiceProvider { get; }

    /// <summary>
    /// 任务实现类型
    /// </summary>
    public Type JobType { get; }

    /// <summary>
    /// 任务参数
    /// </summary>
    public object? JobArgs { get; }

    /// <summary>
    /// 取消令牌
    /// </summary>
    public CancellationToken CancellationToken { get; }

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="serviceProvider">服务提供者</param>
    /// <param name="jobType">任务实现类型</param>
    /// <param name="jobArgs">任务参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    public JobExecutionContext(IServiceProvider serviceProvider, Type jobType, object? jobArgs, CancellationToken cancellationToken = default)
    {
        ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        JobType = jobType ?? throw new ArgumentNullException(nameof(jobType));
        JobArgs = jobArgs;
        CancellationToken = cancellationToken;
    }
}

namespace Snow.BackgroundJobs;

/// <summary>
/// 后台任务配置: 任务登记表 + worker 运行参数, 对应配置节 <c>BackgroundJobs</c>
/// <para>登记表由模块扫描作业类自动填充, 一般不需要手工调用 <see cref="AddJob(Type)"/>。</para>
/// </summary>
public class BackgroundJobOptions
{
    private readonly Dictionary<Type, Type> _jobTypesByArgsType = [];

    private readonly Dictionary<string, (Type JobType, Type ArgsType)> _jobsByName = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 是否启用内置 worker 的执行, 默认 true
    /// <para>换成调度框架(如 Hangfire)时由那个模块设为 false, 免得内置 worker 空轮询进程内存储。</para>
    /// </summary>
    public bool IsJobExecutionEnabled { get; set; } = true;

    /// <summary>
    /// 轮询间隔, 默认 5 秒
    /// </summary>
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// 单轮最多取多少条任务, 默认 100
    /// </summary>
    public int MaxJobFetchCount { get; set; } = 100;

    /// <summary>
    /// 首次失败后等多久重试, 默认 60 秒; 之后每次翻倍
    /// </summary>
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// 最多尝试几次(含第一次), 超过即放弃, 默认 5
    /// </summary>
    public int MaxRetryCount { get; set; } = 5;

    /// <summary>
    /// 是否启用分布式锁(多实例部署时避免重复消费), 默认 true
    /// </summary>
    public bool UseDistributedLock { get; set; } = true;

    /// <summary>
    /// worker 分布式锁名
    /// </summary>
    public string DistributedLockName { get; set; } = "SnowBackgroundJobWorker";

    /// <summary>
    /// 已登记的作业类型
    /// </summary>
    public IReadOnlyCollection<Type> JobTypes => _jobTypesByArgsType.Values;

    /// <summary>
    /// 登记作业类型
    /// </summary>
    /// <param name="jobType">作业实现类型</param>
    /// <returns>当前选项, 便于链式调用</returns>
    public BackgroundJobOptions AddJob(Type jobType)
    {
        ArgumentNullException.ThrowIfNull(jobType);

        if (!jobType.IsClass || jobType.IsAbstract)
        {
            throw new ArgumentException($"作业类型 {jobType.FullName} 必须是可实例化的类。", nameof(jobType));
        }

        Type argsType = BackgroundJobArgsHelper.GetJobArgsType(jobType);
        string jobName = GetJobName(argsType);

        // 双向唯一: 一个参数类型只能有一个实现, 一个任务名也只能指向一个实现
        if (_jobTypesByArgsType.TryGetValue(argsType, out Type? existingByArgs) && existingByArgs != jobType)
        {
            throw new InvalidOperationException($"参数类型 {argsType.FullName} 已登记作业 {existingByArgs.FullName}, 不能再登记 {jobType.FullName}。");
        }

        if (_jobsByName.TryGetValue(jobName, out (Type JobType, Type ArgsType) existingByName) && existingByName.JobType != jobType)
        {
            throw new InvalidOperationException($"任务名 {jobName} 已登记作业 {existingByName.JobType.FullName}, 不能再登记 {jobType.FullName}。");
        }

        _jobTypesByArgsType[argsType] = jobType;
        _jobsByName[jobName] = (jobType, argsType);
        return this;
    }

    /// <summary>
    /// 登记作业类型
    /// </summary>
    /// <typeparam name="TJob">作业实现类型</typeparam>
    /// <returns>当前选项, 便于链式调用</returns>
    public BackgroundJobOptions AddJob<TJob>()
    {
        return AddJob(typeof(TJob));
    }

    /// <summary>
    /// 任务名生成规则: 参数类型全名
    /// </summary>
    /// <param name="argsType">参数类型</param>
    /// <returns>任务名</returns>
    public string GetJobName(Type argsType)
    {
        ArgumentNullException.ThrowIfNull(argsType);

        return argsType.FullName ?? argsType.Name;
    }

    /// <summary>
    /// 按参数类型取作业类型
    /// </summary>
    /// <param name="argsType">参数类型</param>
    /// <returns>作业实现类型</returns>
    public Type GetJobType(Type argsType)
    {
        ArgumentNullException.ThrowIfNull(argsType);

        return _jobTypesByArgsType.TryGetValue(argsType, out Type? jobType)
            ? jobType
            : throw new InvalidOperationException($"参数类型 {argsType.FullName} 没有对应的后台任务, 请实现 {typeof(IBackgroundJob<>).Name}<{argsType.Name}> 或 {typeof(IAsyncBackgroundJob<>).Name}<{argsType.Name}>。");
    }

    /// <summary>
    /// 按任务名取作业类型
    /// </summary>
    /// <param name="jobName">任务名</param>
    /// <returns>作业实现类型</returns>
    public Type GetJobType(string jobName)
    {
        return Resolve(jobName).JobType;
    }

    /// <summary>
    /// 按任务名取参数类型
    /// </summary>
    /// <param name="jobName">任务名</param>
    /// <returns>参数类型</returns>
    public Type GetArgsType(string jobName)
    {
        return Resolve(jobName).ArgsType;
    }

    private (Type JobType, Type ArgsType) Resolve(string jobName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jobName);

        return _jobsByName.TryGetValue(jobName, out (Type JobType, Type ArgsType) job)
            ? job
            : throw new InvalidOperationException($"找不到名为 {jobName} 的后台任务。");
    }
}

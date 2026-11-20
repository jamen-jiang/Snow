namespace Snow.BackgroundJobs;

/// <summary>
/// 后台任务配置: 任务注册表(参数类型 → 作业类型, 任务名 → 作业类型)与总开关
/// <para>作业类由模块扫描自动登记, 无需手工 AddJob; 手工登记用于动态生成的类型。</para>
/// </summary>
public class BackgroundJobOptions
{


    private readonly Dictionary<Type, Type> _jobTypesByArgsType = [];

    private readonly Dictionary<string, Type> _jobTypesByName = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 是否启用任务执行(关闭后仍可入队, 只是没有 worker 消费)
    /// </summary>
    public bool IsJobExecutionEnabled { get; set; } = true;

    /// <summary>
    /// 任务名生成规则, 默认取参数类型全名
    /// </summary>
    public Func<Type, string> GetJobName { get; set; } = static argsType => argsType.FullName ?? argsType.Name;

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

        if (_jobTypesByArgsType.TryGetValue(argsType, out Type? existing) && existing != jobType)
        {
            throw new InvalidOperationException($"参数类型 {argsType.FullName} 已登记作业 {existing.FullName}, 不能再登记 {jobType.FullName}。");
        }

        _jobTypesByArgsType[argsType] = jobType;
        _jobTypesByName[jobName] = jobType;
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
        ArgumentException.ThrowIfNullOrWhiteSpace(jobName);

        return _jobTypesByName.TryGetValue(jobName, out Type? jobType)
            ? jobType
            : throw new InvalidOperationException($"找不到名为 {jobName} 的后台任务。");
    }

    /// <summary>
    /// 取某条任务记录的参数类型
    /// </summary>
    /// <param name="jobName">任务名</param>
    /// <returns>参数类型</returns>
    public Type GetArgsType(string jobName)
    {
        return BackgroundJobArgsHelper.GetJobArgsType(GetJobType(jobName));
    }
}

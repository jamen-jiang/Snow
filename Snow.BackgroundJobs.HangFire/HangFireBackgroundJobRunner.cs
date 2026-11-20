using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Snow.BackgroundJobs;
using Snow.DependencyInjection;

namespace Snow.BackgroundJobs.HangFire;

/// <summary>
/// Hangfire 侧的任务执行入口: 只接任务名与序列化后的参数, 具体作业类型由注册表还原
/// <para>这样队列式任务不必为每种参数类型写一个 Hangfire 作业类。</para>
/// </summary>
public sealed class HangFireBackgroundJobRunner : ITransientDependency
{
    private readonly IServiceProvider _serviceProvider;

    private readonly IOptions<BackgroundJobOptions> _options;

    private readonly IBackgroundJobSerializer _serializer;

    private readonly IBackgroundJobExecuter _executer;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="serviceProvider">作用域内服务提供者</param>
    /// <param name="options">任务配置</param>
    /// <param name="serializer">参数序列化</param>
    /// <param name="executer">任务执行器</param>
    public HangFireBackgroundJobRunner(IServiceProvider serviceProvider, IOptions<BackgroundJobOptions> options, IBackgroundJobSerializer serializer, IBackgroundJobExecuter executer)
    {
        _serviceProvider = serviceProvider;
        _options = options;
        _serializer = serializer;
        _executer = executer;
    }

    /// <summary>
    /// 执行任务
    /// </summary>
    /// <param name="jobName">任务名</param>
    /// <param name="jobArgs">序列化后的任务参数</param>
    /// <returns>表示执行过程的任务</returns>
    public async Task RunAsync(string jobName, string jobArgs)
    {
        BackgroundJobOptions options = _options.Value;
        Type argsType = options.GetArgsType(jobName);
        Type jobType = options.GetJobType(jobName);
        object? args = _serializer.Deserialize(jobArgs, argsType);

        await _executer.ExecuteAsync(new JobExecutionContext(_serviceProvider, jobType, args)).ConfigureAwait(false);
    }
}

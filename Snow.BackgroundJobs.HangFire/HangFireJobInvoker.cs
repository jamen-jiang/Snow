using Microsoft.Extensions.Options;
using Snow.BackgroundJobs;

namespace Snow.BackgroundJobs.HangFire;

/// <summary>
/// Hangfire 作业入口: 队列任务回放时只认这个类型, 具体任务名与参数由 Hangfire 传进来
/// </summary>
public sealed class HangFireJobInvoker
{
    private readonly IServiceProvider _serviceProvider;

    private readonly BackgroundJobOptions _options;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="serviceProvider">作用域内服务提供者</param>
    /// <param name="options">任务配置</param>
    public HangFireJobInvoker(IServiceProvider serviceProvider, IOptions<BackgroundJobOptions> options)
    {
        _serviceProvider = serviceProvider;
        _options = options.Value;
    }

    /// <summary>
    /// 执行一条队列任务
    /// </summary>
    /// <param name="jobName">任务名</param>
    /// <param name="jobArgs">序列化后的任务参数</param>
    public Task RunAsync(string jobName, string jobArgs)
    {
        return BackgroundJobInvoker.RunAsync(_serviceProvider, _options, jobName, jobArgs);
    }
}

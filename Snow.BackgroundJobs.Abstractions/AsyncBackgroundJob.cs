using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Snow.BackgroundJobs;

/// <summary>
/// 异步后台任务基类: 提供 <see cref="Logger"/>, 业务侧只需实现 <see cref="ExecuteAsync"/>
/// </summary>
/// <typeparam name="TArgs">任务参数类型</typeparam>
public abstract class AsyncBackgroundJob<TArgs> : IAsyncBackgroundJob<TArgs>
{
    /// <summary>
    /// 日志
    /// </summary>
    public ILogger Logger { get; set; } = NullLogger.Instance;

    /// <inheritdoc />
    public abstract Task ExecuteAsync(TArgs args, CancellationToken cancellationToken = default);
}

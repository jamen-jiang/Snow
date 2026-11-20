using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Snow.BackgroundJobs;

/// <summary>
/// 后台执行端口的兜底实现: 没有引入调度框架时只记日志, 不阻塞调用方
/// </summary>
public sealed class NullBackgroundExecutor : IBackgroundExecutor
{
    private readonly ILogger<NullBackgroundExecutor> _logger;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="logger">日志</param>
    public NullBackgroundExecutor(ILogger<NullBackgroundExecutor>? logger = null)
    {
        _logger = logger ?? NullLogger<NullBackgroundExecutor>.Instance;
    }

    /// <inheritdoc />
    public Task DispatchAsync<TCommand>(TCommand payload)
    {
        ArgumentNullException.ThrowIfNull(payload);

        _logger.LogWarning("未引入调度框架, 载荷 {Payload} 未投递到后台。", typeof(TCommand).FullName);

        return Task.CompletedTask;
    }
}

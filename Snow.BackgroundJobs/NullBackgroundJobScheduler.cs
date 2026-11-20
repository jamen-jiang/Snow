using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Snow.BackgroundJobs;

/// <summary>
/// 调度端口的空实现: 没有引入 Hangfire/Quartz 时兜底, 调用只记日志不抛异常
/// <para>引入 <c>Snow.BackgroundJobs.HangFire</c> 后由模块 Replace 成真实实现。</para>
/// </summary>
public sealed class NullBackgroundJobScheduler : IBackgroundJobScheduler
{
    private readonly ILogger<NullBackgroundJobScheduler> _logger;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="logger">日志</param>
    public NullBackgroundJobScheduler(ILogger<NullBackgroundJobScheduler>? logger = null)
    {
        _logger = logger ?? NullLogger<NullBackgroundJobScheduler>.Instance;
    }

    /// <inheritdoc />
    public void ScheduleRecurring(string id, string cron, string beanName, string methodName)
    {
        _logger.LogWarning("未引入调度框架, 周期任务 {Id}({Bean}.{Method}, {Cron}) 未注册。", id, beanName, methodName, cron);
    }

    /// <inheritdoc />
    public void DeleteRecurring(string id)
    {
        _logger.LogWarning("未引入调度框架, 周期任务 {Id} 未删除。", id);
    }

    /// <inheritdoc />
    public void TriggerOnce(string beanName, string methodName)
    {
        _logger.LogWarning("未引入调度框架, 一次性任务 {Bean}.{Method} 未触发。", beanName, methodName);
    }
}

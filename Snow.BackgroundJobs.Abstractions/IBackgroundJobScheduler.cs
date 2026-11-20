namespace Snow.BackgroundJobs;

/// <summary>
/// 调度端口: 周期任务与一次性触发的抽象, 由调度框架适配(Hangfire / Quartz)
/// <para>与队列式 <see cref="IBackgroundJobManager"/> 互补: 前者按 cron 或立即触发方法, 后者按参数类型入队执行。</para>
/// </summary>
public interface IBackgroundJobScheduler
{
    /// <summary>
    /// 注册周期任务(已存在则更新)
    /// </summary>
    /// <param name="id">任务 Id(调度框架内的唯一标识)</param>
    /// <param name="cron">cron 表达式</param>
    /// <param name="beanName">目标类名(服务注册名)</param>
    /// <param name="methodName">目标方法名</param>
    void ScheduleRecurring(string id, string cron, string beanName, string methodName);

    /// <summary>
    /// 删除周期任务
    /// </summary>
    /// <param name="id">任务 Id</param>
    void DeleteRecurring(string id);

    /// <summary>
    /// 立即执行一次任务逻辑(不影响周期调度)
    /// </summary>
    /// <param name="beanName">目标类名(服务注册名)</param>
    /// <param name="methodName">目标方法名</param>
    void TriggerOnce(string beanName, string methodName);
}

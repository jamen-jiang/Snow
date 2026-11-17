namespace Rongia.Kernel.Application.Port.Job;

/// <summary>
/// 后台任务调度端口:封装底层调度框架,application 层只依赖此接口
/// </summary>
public interface IBackgroundJob
{
    /// <summary>
    /// 注册周期性任务(存在则更新)
    /// </summary>
    /// <param name="id">任务Id(作为调度框架内的唯一标识)</param>
    /// <param name="cron">cron 表达式</param>
    /// <param name="beanName">目标类名(服务注册名)</param>
    /// <param name="methodName">目标方法名</param>
    void ScheduleRecurring(string id, string cron, string beanName, string methodName);

    /// <summary>
    /// 删除周期性任务
    /// </summary>
    /// <param name="id">任务Id</param>
    void DeleteRecurring(string id);

    /// <summary>
    /// 立即执行一次任务逻辑(不影响原有的周期性调度)
    /// </summary>
    /// <param name="beanName">目标类名(服务注册名)</param>
    /// <param name="methodName">目标方法名</param>
    void TriggerOnce(string beanName, string methodName);
}

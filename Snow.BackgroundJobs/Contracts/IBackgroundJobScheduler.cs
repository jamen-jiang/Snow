using System.Linq.Expressions;

namespace Snow.BackgroundJobs;

/// <summary>
/// 调度入口: 按 cron 周期执行, 或立刻跑一次某个服务的方法
/// <para>用表达式而不是类名/方法名字符串 —— 调什么方法在编译期就定死, 不存在"名字找不到类型"这一类运行时错误。</para>
/// <para>本包不提供实现: 装 Snow.BackgroundJobs.HangFire 才有; 没有实现时注入会直接报错, 而不是静默什么都不做。</para>
/// </summary>
public interface IBackgroundJobScheduler
{
    /// <summary>
    /// 注册周期任务(同 id 已存在则更新)
    /// </summary>
    /// <typeparam name="TJob">承载方法的服务类型, 需能被 DI 解析</typeparam>
    /// <param name="id">周期任务 Id</param>
    /// <param name="cron">cron 表达式</param>
    /// <param name="job">要周期执行的方法, 例如 <c>s =&gt; s.RunAsync()</c></param>
    void ScheduleRecurring<TJob>(string id, string cron, Expression<Func<TJob, Task>> job);

    /// <summary>
    /// 删除周期任务
    /// </summary>
    /// <param name="id">周期任务 Id</param>
    void DeleteRecurring(string id);

    /// <summary>
    /// 立刻执行一次, 不影响周期调度
    /// </summary>
    /// <typeparam name="TJob">承载方法的服务类型</typeparam>
    /// <param name="job">要执行的方法</param>
    void TriggerOnce<TJob>(Expression<Func<TJob, Task>> job);
}

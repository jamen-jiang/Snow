using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Snow.BackgroundJobs;

/// <summary>
/// 任务入队入口: 写法跟 Hangfire 一致, 直接把要执行的方法写成 lambda
/// <code>
/// await manager.EnqueueAsync&lt;IMailService&gt;(mail => mail.SendAsync("a@b.c"));
/// await manager.EnqueueAsync(() =&gt; service.RefreshAsync(), delay: TimeSpan.FromMinutes(5));
/// </code>
/// <para>方法必须是 public, 返回 void/Task/ValueTask; 参数里可以带 <see cref="CancellationToken"/>, 执行时由 worker 注入。</para>
/// </summary>
public interface IBackgroundJobManager
{
    /// <summary>
    /// 入队一个无返回值的静态/实例方法调用
    /// </summary>
    /// <param name="job">任务表达式</param>
    /// <param name="delay">延迟多久执行, null 表示立即</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务标识</returns>
    Task<string> EnqueueAsync(Expression<Action> job, TimeSpan? delay = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 入队一个异步方法调用
    /// </summary>
    /// <param name="job">任务表达式</param>
    /// <param name="delay">延迟多久执行, null 表示立即</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务标识</returns>
    Task<string> EnqueueAsync(Expression<Func<Task>> job, TimeSpan? delay = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 入队一个服务上的无返回值方法调用
    /// </summary>
    /// <typeparam name="TService">服务类型, 需能被容器解析</typeparam>
    /// <param name="job">任务表达式, 例如 <c>s =&gt; s.Do()</c></param>
    /// <param name="delay">延迟多久执行, null 表示立即</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务标识</returns>
    Task<string> EnqueueAsync<TService>(Expression<Action<TService>> job, TimeSpan? delay = null, CancellationToken cancellationToken = default)
        where TService : class;

    /// <summary>
    /// 入队一个服务上的异步方法调用
    /// </summary>
    /// <typeparam name="TService">服务类型, 需能被容器解析</typeparam>
    /// <param name="job">任务表达式, 例如 <c>s =&gt; s.DoAsync()</c></param>
    /// <param name="delay">延迟多久执行, null 表示立即</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务标识</returns>
    Task<string> EnqueueAsync<TService>(Expression<Func<TService, Task>> job, TimeSpan? delay = null, CancellationToken cancellationToken = default)
        where TService : class;
}

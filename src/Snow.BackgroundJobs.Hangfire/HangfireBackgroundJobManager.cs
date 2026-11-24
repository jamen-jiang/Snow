using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using global::Hangfire;
using global::Hangfire.Common;
using global::Hangfire.States;
using Snow.DependencyInjection;

namespace Snow.BackgroundJobs.Hangfire;

/// <summary>
/// Hangfire 入队实现: lambda 直接交给 Hangfire 解析, 面板里看到的就是真实的服务与方法
/// <code>
/// await manager.EnqueueAsync&lt;IMailService&gt;(mail =&gt; mail.SendAsync("a@b.c"));
/// await manager.EnqueueAsync(() =&gt; service.RefreshAsync(), delay: TimeSpan.FromMinutes(5));
/// </code>
/// <para>参数在入队这一刻求值并交给 Hangfire 序列化。</para>
/// </summary>
[TransientService(ReplaceServices = true)]
public sealed class HangfireBackgroundJobManager : IBackgroundJobManager
{
    private readonly IBackgroundJobClient _backgroundJobClient;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="backgroundJobClient">Hangfire 入队客户端</param>
    public HangfireBackgroundJobManager(IBackgroundJobClient backgroundJobClient)
    {
        _backgroundJobClient = backgroundJobClient;
    }

    /// <inheritdoc />
    /// <param name="job">作业表达式</param>
    /// <param name="delay">延迟时间, 可为 null</param>
    /// <param name="cancellationToken">取消令牌</param>
    public Task<string> EnqueueAsync(Expression<Action> job, TimeSpan? delay = null, CancellationToken cancellationToken = default)
    {
        return EnqueueAsync(Job.FromExpression(job), delay);
    }

    /// <inheritdoc />
    /// <param name="job">作业表达式</param>
    /// <param name="delay">延迟时间, 可为 null</param>
    /// <param name="cancellationToken">取消令牌</param>
    public Task<string> EnqueueAsync(Expression<Func<Task>> job, TimeSpan? delay = null, CancellationToken cancellationToken = default)
    {
        return EnqueueAsync(Job.FromExpression(job), delay);
    }

    /// <inheritdoc />
    /// <param name="job">作业表达式</param>
    /// <param name="delay">延迟时间, 可为 null</param>
    /// <param name="cancellationToken">取消令牌</param>
    public Task<string> EnqueueAsync<TService>(Expression<Action<TService>> job, TimeSpan? delay = null, CancellationToken cancellationToken = default)
        where TService : class
    {
        return EnqueueAsync(Job.FromExpression(job), delay);
    }

    /// <inheritdoc />
    /// <param name="job">作业表达式</param>
    /// <param name="delay">延迟时间, 可为 null</param>
    /// <param name="cancellationToken">取消令牌</param>
    public Task<string> EnqueueAsync<TService>(Expression<Func<TService, Task>> job, TimeSpan? delay = null, CancellationToken cancellationToken = default)
        where TService : class
    {
        return EnqueueAsync(Job.FromExpression(job), delay);
    }

    private Task<string> EnqueueAsync(Job job, TimeSpan? delay)
    {
        IState state = delay.HasValue
            ? new ScheduledState(delay.Value)
            : new EnqueuedState();

        return Task.FromResult(_backgroundJobClient.Create(job, state));
    }
}
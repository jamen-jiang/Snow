using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Snow.DistributedLocking;
using Snow.Timing;

namespace Snow.BackgroundJobs;

/// <summary>
/// 内置任务轮询器: 定时取待执行任务并执行, 失败按次数退避重试
/// </summary>
public sealed class BackgroundJobWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;

    private readonly IServiceScopeFactory _scopeFactory;

    private readonly BackgroundJobOptions _options;

    private readonly IClock _clock;

    private readonly ILogger<BackgroundJobWorker> _logger;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="serviceProvider">根服务提供器, 用于取分布式锁</param>
    /// <param name="options">任务配置</param>
    /// <param name="clock">时间提供器</param>
    /// <param name="logger">日志</param>
    public BackgroundJobWorker(
        IServiceProvider serviceProvider,
        IOptions<BackgroundJobOptions> options,
        IClock clock,
        ILogger<BackgroundJobWorker>? logger = null)
    {
        _serviceProvider = serviceProvider;
        _scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
        _options = options.Value;
        _clock = clock;
        _logger = logger ?? NullLogger<BackgroundJobWorker>.Instance;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogDebug("后台任务已关闭, 内置 worker 不启动。");

            return;
        }

        // 不阻塞宿主启动
        await Task.Yield();

        while (!stoppingToken.IsCancellationRequested)
        {
            TimeSpan nextDelay = _options.PollInterval;

            try
            {
                nextDelay = await RunOnceAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "后台任务轮询失败。");
            }

            try
            {
                await Task.Delay(nextDelay, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    /// <summary>
    /// 执行一轮轮询
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>下一轮之前的等待时长</returns>
    private async Task<TimeSpan> RunOnceAsync(CancellationToken cancellationToken)
    {
        IDistributedLock? distributedLock = _options.UseDistributedLock ? _serviceProvider.GetService<IDistributedLock>() : null;

        if (distributedLock is null)
        {
            await ExecuteWaitingJobsAsync(cancellationToken).ConfigureAwait(false);

            return _options.PollInterval;
        }

        await using IDistributedLockHandle? handle = await distributedLock
            .TryAcquireAsync(_options.DistributedLockName, TimeSpan.Zero, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        // 没抢到锁说明别的实例在跑: 退避一会儿再来, 否则这里会变成热循环
        if (handle is null)
        {
            return _options.LockUnavailableDelay;
        }

        await ExecuteWaitingJobsAsync(cancellationToken).ConfigureAwait(false);

        return _options.PollInterval;
    }

    private async Task ExecuteWaitingJobsAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        IBackgroundJobStore store = scope.ServiceProvider.GetRequiredService<IBackgroundJobStore>();

        List<BackgroundJobInfo> jobs = await store
            .GetWaitingJobsAsync(_options.MaxJobFetchCount, cancellationToken)
            .ConfigureAwait(false);

        foreach (BackgroundJobInfo job in jobs)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            await ExecuteJobAsync(scope.ServiceProvider, store, job, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task ExecuteJobAsync(IServiceProvider scopedProvider, IBackgroundJobStore store, BackgroundJobInfo job, CancellationToken cancellationToken)
    {
        job.TryCount++;

        try
        {
            await scopedProvider.GetRequiredService<IBackgroundJobInvoker>().RunAsync(job, cancellationToken).ConfigureAwait(false);
            await store.DeleteAsync(job.Id, cancellationToken).ConfigureAwait(false);
        }
        catch (BackgroundJobExecutionException exception)
        {
            await HandleFailureAsync(store, job, exception, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // 停机打断, 不算失败: 保持原样等下次启动
            throw;
        }
        catch (Exception exception)
        {
            // 找不到类型、参数反序列化失败、没有执行方法这类错误重试多少次都一样, 直接丢弃
            _logger.LogError(exception, "后台任务 {JobName}({JobId}) 无法执行, 已丢弃。", job.DisplayName, job.Id);

            await store.DeleteAsync(job.Id, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// 任务内部失败: 没到上限就退避重试, 到了就丢弃
    /// </summary>
    private async Task HandleFailureAsync(IBackgroundJobStore store, BackgroundJobInfo job, Exception exception, CancellationToken cancellationToken)
    {
        if (job.TryCount >= _options.MaxRetryCount)
        {
            _logger.LogError(exception, "后台任务 {JobName}({JobId}) 第 {TryCount} 次执行失败, 已达重试上限, 丢弃。", job.DisplayName, job.Id, job.TryCount);

            await store.DeleteAsync(job.Id, cancellationToken).ConfigureAwait(false);

            return;
        }

        TimeSpan delay = CalculateRetryDelay(job.TryCount);

        _logger.LogWarning(exception, "后台任务 {JobName}({JobId}) 第 {TryCount} 次执行失败, {Delay} 后重试。", job.DisplayName, job.Id, job.TryCount, delay);

        job.NextTryTime = _clock.UtcNow.Add(delay);

        await store.UpdateAsync(job, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 指数退避: 首次等待 RetryDelay, 之后每次乘 WaitFactor, 上限一天
    /// </summary>
    private TimeSpan CalculateRetryDelay(int tryCount)
    {
        double seconds = _options.RetryDelay.TotalSeconds * Math.Pow(_options.WaitFactor, Math.Max(0, tryCount - 1));

        return TimeSpan.FromSeconds(Math.Min(seconds, TimeSpan.FromDays(1).TotalSeconds));
    }
}

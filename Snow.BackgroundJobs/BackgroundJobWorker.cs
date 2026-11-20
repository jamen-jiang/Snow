using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Snow.DistributedLocking;

namespace Snow.BackgroundJobs;

/// <summary>
/// 内置 worker: 定时轮询存储, 取到点任务交给 <see cref="BackgroundJobInvoker"/>
/// <para>成功即删除; 失败按 <see cref="BackgroundJobOptions.RetryDelay"/> 指数退避回写下次执行时间,
/// 尝试次数达到 <see cref="BackgroundJobOptions.MaxRetryCount"/> 即放弃并删除。</para>
/// </summary>
public sealed class BackgroundJobWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;

    private readonly IServiceScopeFactory _scopeFactory;

    private readonly BackgroundJobOptions _options;

    private readonly ILogger<BackgroundJobWorker> _logger;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="serviceProvider">根服务提供者</param>
    /// <param name="options">任务配置</param>
    /// <param name="logger">日志</param>
    public BackgroundJobWorker(IServiceProvider serviceProvider, IOptions<BackgroundJobOptions> options, ILogger<BackgroundJobWorker>? logger = null)
    {
        _serviceProvider = serviceProvider;
        _scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
        _options = options.Value;
        _logger = logger ?? NullLogger<BackgroundJobWorker>.Instance;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // 执行交给调度框架时不空转: 任务由那边的服务端消费
        if (!_options.IsJobExecutionEnabled)
        {
            _logger.LogDebug("后台任务执行已关闭, 内置 worker 不启动。");
            return;
        }

        // 让出线程, 避免阻塞宿主启动
        await Task.Yield();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunOnceAsync(stoppingToken).ConfigureAwait(false);
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
                await Task.Delay(_options.PollInterval, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        IDistributedLock? distributedLock = _options.UseDistributedLock
            ? _serviceProvider.GetService<IDistributedLock>()
            : null;

        // 显式零等待: 拿不到说明别的实例正在消费, 本轮直接跳过
        await using IDistributedLockHandle? handle = distributedLock is null
            ? null
            : await distributedLock
                .TryAcquireAsync(_options.DistributedLockName, TimeSpan.Zero, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

        if (distributedLock is not null && handle is null)
        {
            return;
        }

        await ExecuteWaitingJobsAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task ExecuteWaitingJobsAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();

        IBackgroundJobStore store = scope.ServiceProvider.GetRequiredService<IBackgroundJobStore>();
        List<BackgroundJobInfo> jobs = await store.GetWaitingJobsAsync(_options.MaxJobFetchCount, cancellationToken).ConfigureAwait(false);

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
            await BackgroundJobInvoker.RunAsync(scopedProvider, _options, job.JobName, job.JobArgs, cancellationToken).ConfigureAwait(false);
            await store.DeleteAsync(job.Id, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            bool abandoned = job.TryCount >= _options.MaxRetryCount;

            if (abandoned)
            {
                _logger.LogError(exception, "后台任务 {JobName}({JobId}) 第 {TryCount} 次执行失败, 已达重试上限, 放弃。", job.JobName, job.Id, job.TryCount);
                job.IsAbandoned = true;
            }
            else
            {
                job.NextTryTime = DateTime.UtcNow.Add(GetRetryDelay(job.TryCount));
                _logger.LogWarning(exception, "后台任务 {JobName}({JobId}) 第 {TryCount} 次执行失败, {Delay} 后重试。", job.JobName, job.Id, job.TryCount, GetRetryDelay(job.TryCount));
            }

            await store.UpdateAsync(job, cancellationToken).ConfigureAwait(false);
        }
    }

    private TimeSpan GetRetryDelay(int tryCount)
    {
        return TimeSpan.FromSeconds(_options.RetryDelay.TotalSeconds * Math.Pow(2, tryCount - 1));
    }
}

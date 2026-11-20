using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Snow.DistributedLocking;

namespace Snow.BackgroundJobs;

/// <summary>
/// 任务 worker: 定时轮询存储, 取待执行任务交给 <see cref="IBackgroundJobExecuter"/>
/// <para>成功即删除; 失败按指数退避回写下次执行时间; 超过 <see cref="BackgroundJobWorkerOptions.Timeout"/> 或参数反序列化失败则放弃。</para>
/// </summary>
public sealed class BackgroundJobWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;

    private readonly BackgroundJobOptions _jobOptions;

    private readonly BackgroundJobWorkerOptions _workerOptions;

    private readonly ILogger<BackgroundJobWorker> _logger;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="serviceProvider">根服务提供者</param>
    /// <param name="jobOptions">任务配置</param>
    /// <param name="workerOptions">worker 配置</param>
    /// <param name="logger">日志</param>
    public BackgroundJobWorker(IServiceProvider serviceProvider, IOptions<BackgroundJobOptions> jobOptions, IOptions<BackgroundJobWorkerOptions> workerOptions, ILogger<BackgroundJobWorker>? logger = null)
    {
        _serviceProvider = serviceProvider;
        _jobOptions = jobOptions.Value;
        _workerOptions = workerOptions.Value;
        _logger = logger ?? NullLogger<BackgroundJobWorker>.Instance;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
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
                await Task.Delay(_workerOptions.PollInterval, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        if (!_jobOptions.IsJobExecutionEnabled)
        {
            return;
        }

        IDistributedLock? distributedLock = _workerOptions.UseDistributedLock ? _serviceProvider.GetService<IDistributedLock>() : null;

        if (distributedLock is null)
        {
            await ExecuteWaitingJobsAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        // 显式零等待: 拿不到说明别的实例正在消费, 本轮回 Task.Delay 再来。
        // 不传 timeout 现在是「一直等」, 挂在这儿等会把「跳过本轮」变成「排队轮转」。
        await using IDistributedLockHandle? handle = await distributedLock
            .TryAcquireAsync(_workerOptions.DistributedLockName, TimeSpan.Zero, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        // 拿不到锁说明别的实例在消费, 本轮直接跳过
        if (handle is null)
        {
            return;
        }

        await ExecuteWaitingJobsAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task ExecuteWaitingJobsAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = _serviceProvider.CreateScope();

        IBackgroundJobStore store = scope.ServiceProvider.GetRequiredService<IBackgroundJobStore>();
        List<BackgroundJobInfo> jobs = await store.GetWaitingJobsAsync(_workerOptions.MaxJobFetchCount, cancellationToken).ConfigureAwait(false);

        if (jobs.Count == 0)
        {
            return;
        }

        IBackgroundJobExecuter executer = scope.ServiceProvider.GetRequiredService<IBackgroundJobExecuter>();
        IBackgroundJobSerializer serializer = scope.ServiceProvider.GetRequiredService<IBackgroundJobSerializer>();

        foreach (BackgroundJobInfo job in jobs)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            await ExecuteJobAsync(scope.ServiceProvider, store, executer, serializer, job, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task ExecuteJobAsync(IServiceProvider serviceProvider, IBackgroundJobStore store, IBackgroundJobExecuter executer, IBackgroundJobSerializer serializer, BackgroundJobInfo job, CancellationToken cancellationToken)
    {
        job.TryCount++;
        job.LastTryTime = DateTime.UtcNow;

        try
        {
            Type argsType = _jobOptions.GetArgsType(job.JobName);
            Type jobType = _jobOptions.GetJobType(job.JobName);
            object? args = serializer.Deserialize(job.JobArgs, argsType);

            await executer.ExecuteAsync(new JobExecutionContext(serviceProvider, jobType, args, cancellationToken)).ConfigureAwait(false);
            await store.DeleteAsync(job.Id, cancellationToken).ConfigureAwait(false);
        }
        catch (BackgroundJobExecutionException exception)
        {
            _logger.LogError(exception, "后台任务 {JobName}({JobId}) 第 {TryCount} 次执行失败。", job.JobName, job.Id, job.TryCount);
            await RescheduleAsync(store, job, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            // 参数反序列化失败、任务名找不到实现等: 重试也不会好, 直接放弃
            _logger.LogError(exception, "后台任务 {JobName}({JobId}) 无法执行, 已放弃。", job.JobName, job.Id);
            job.IsAbandoned = true;
            await store.UpdateAsync(job, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task RescheduleAsync(IBackgroundJobStore store, BackgroundJobInfo job, CancellationToken cancellationToken)
    {
        double waitSeconds = _workerOptions.FirstWaitDuration.TotalSeconds * Math.Pow(_workerOptions.WaitFactor, job.TryCount - 1);
        DateTime nextTryTime = (job.LastTryTime ?? DateTime.UtcNow).AddSeconds(waitSeconds);

        if (nextTryTime - job.CreationTime > _workerOptions.Timeout)
        {
            job.IsAbandoned = true;
        }
        else
        {
            job.NextTryTime = nextTryTime;
        }

        await store.UpdateAsync(job, cancellationToken).ConfigureAwait(false);
    }
}

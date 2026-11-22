using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Snow.DependencyInjection;
using Snow.DistributedLocking;
using Snow.Modularity;
using Snow.Timing;

namespace Snow.BackgroundJobs;

/// <summary>
/// 后台任务模块: 登记存储、入队器、执行器与内置 worker
/// <para>入队用 lambda, 不需要扫描"作业类型": 要执行什么方法由调用方在表达式里写清楚。</para>
/// <para>依赖 <see cref="SnowTimingModule"/>: worker 与任务记录都用 <see cref="IClock"/> 取时间。</para>
/// </summary>
[DependsOn(typeof(SnowDistributedLockModule), typeof(SnowTimingModule))]
public sealed class BackgroundJobsModule : SnowModule
{
    /// <inheritdoc />
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        IServiceCollection services = context.Services;

        // 存储、入队器、执行器都由实现类上的生命周期注解登记; worker 是 HostedService, 只能显式登记
        Configure<BackgroundJobOptions>(services.GetConfiguration().GetSection("BackgroundJobs"));

        services.AddHostedService<BackgroundJobWorker>();
    }
}

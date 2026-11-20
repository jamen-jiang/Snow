using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Snow.DependencyInjection;
using Snow.DistributedLocking;
using Snow.Modularity;

namespace Snow.BackgroundJobs;

/// <summary>
/// 后台任务模块: 注册任务存储、序列化、执行器、管理器与轮询 worker, 并扫描本模块程序集内的作业类
/// <para>调度框架(Hangfire/Quartz)是可插拔适配: 未引入时由 <see cref="NullBackgroundJobScheduler"/> 与 <see cref="NullBackgroundExecutor"/> 兜底。</para>
/// </summary>
[DependsOn(typeof(BackgroundJobsAbstractionsModule), typeof(SnowDistributedLockModule))]
public sealed class BackgroundJobsModule : SnowModule
{
    /// <inheritdoc />
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        IServiceCollection services = context.Services;

        services.TryAddSingleton<IBackgroundJobStore, InMemoryBackgroundJobStore>();
        services.TryAddSingleton<IBackgroundJobSerializer, JsonBackgroundJobSerializer>();
        services.TryAddTransient<IBackgroundJobExecuter, BackgroundJobExecuter>();
        services.TryAddTransient<IBackgroundJobManager, DefaultBackgroundJobManager>();
        services.TryAddSingleton<IBackgroundJobScheduler, NullBackgroundJobScheduler>();
        services.TryAddSingleton<IBackgroundExecutor, NullBackgroundExecutor>();

        IConfiguration configuration = context.Services.GetConfiguration();
        Configure<BackgroundJobOptions>(configuration.GetSection("BackgroundJobs"));
        Configure<BackgroundJobWorkerOptions>(configuration.GetSection("BackgroundJobs"));

        // 作业类靠扫描自动登记: 约定注册只认带生命周期标记的类, 作业类不该被强制加标记
        IModuleContainer moduleContainer = services.GetSingletonInstance<IModuleContainer>();
        Type[] jobTypes = FindJobTypes(moduleContainer);

        Configure<BackgroundJobOptions>(options =>
        {
            foreach (Type jobType in jobTypes)
            {
                options.AddJob(jobType);
            }
        });

        services.AddHostedService<BackgroundJobWorker>();
    }

    private static Type[] FindJobTypes(IModuleContainer moduleContainer)
    {
        return moduleContainer.Modules
            .SelectMany(module => module.AllAssemblies)
            .Distinct()
            .SelectMany(AssemblyTypeCache.GetTypes)
            .Where(BackgroundJobArgsHelper.IsBackgroundJob)
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .ToArray();
    }
}

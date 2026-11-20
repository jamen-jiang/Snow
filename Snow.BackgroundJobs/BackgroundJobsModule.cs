using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Snow.DependencyInjection;
using Snow.DistributedLocking;
using Snow.Modularity;

namespace Snow.BackgroundJobs;

/// <summary>
/// 后台任务模块: 注册存储、入口实现与内置 worker, 并扫描模块程序集里的作业类
/// </summary>
[DependsOn(typeof(SnowDistributedLockModule))]
public sealed class BackgroundJobsModule : SnowModule
{
    /// <inheritdoc />
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        IServiceCollection services = context.Services;

        services.TryAddSingleton<IBackgroundJobStore, InMemoryBackgroundJobStore>();
        services.TryAddTransient<IBackgroundJobManager, DefaultBackgroundJobManager>();

        Configure<BackgroundJobOptions>(services.GetConfiguration().GetSection("BackgroundJobs"));

        // 作业类靠扫描登记: 约定注册只认带生命周期标记的类, 作业类不该被强制加标记
        Type[] jobTypes = FindJobTypes(services.GetSingletonInstance<IModuleContainer>());
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
            .Where(IsJobType)
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .ToArray();
    }

    /// <summary>
    /// 作业类判定: 实现 IBackgroundJob&lt;TArgs&gt; 或 IAsyncBackgroundJob&lt;TArgs&gt; 的具体类
    /// </summary>
    private static bool IsJobType(Type type)
    {
        if (!type.IsClass || type.IsAbstract)
        {
            return false;
        }

        return type.GetInterfaces().Any(interfaceType =>
            interfaceType.IsGenericType &&
            (interfaceType.GetGenericTypeDefinition() == typeof(IBackgroundJob<>) ||
             interfaceType.GetGenericTypeDefinition() == typeof(IAsyncBackgroundJob<>)));
    }
}

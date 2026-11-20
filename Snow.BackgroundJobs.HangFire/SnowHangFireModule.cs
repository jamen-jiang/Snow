using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Snow.BackgroundJobs;
using Snow.DependencyInjection;
using Snow.Modularity;
using global::Hangfire;
using global::Hangfire.InMemory;

namespace Snow.BackgroundJobs.HangFire;

/// <summary>
/// Hangfire 集成模块: 注册 Hangfire 服务端与存储, 并用 Hangfire 实现替换入队与调度入口
/// <para>宿主已自行 AddHangfire(配置持久化存储)时不再重复注册, 只接管入口。</para>
/// </summary>
[DependsOn(typeof(BackgroundJobsModule))]
public sealed class SnowHangFireModule : SnowModule
{
    /// <inheritdoc />
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        IConfigurationSection section = context.Services.GetConfiguration().GetSection("HangFire");
        SnowHangFireOptions hangfireOptions = new();
        section.Bind(hangfireOptions);
        Configure<SnowHangFireOptions>(section);

        if (!context.Services.Any(descriptor => descriptor.ServiceType == typeof(IGlobalConfiguration)))
        {
            context.Services.AddHangfire(configuration =>
            {
                configuration
                    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                    .UseSimpleAssemblyNameTypeSerializer()
                    .UseRecommendedSerializerSettings();

                if (hangfireOptions.UseInMemoryStorage)
                {
                    configuration.UseInMemoryStorage();
                }
            });
        }

        if (hangfireOptions.EnableServer)
        {
            // 直接写进服务端选项: 走委托而不是 IOptions, 保证服务端构建时拿到的就是这份值
            context.Services.AddHangfireServer(options =>
            {
                // 调度器轮询: 决定延迟任务与重试任务多久出队
                options.SchedulePollingInterval = TimeSpan.FromSeconds(Math.Max(1, hangfireOptions.PollingIntervalSeconds));

                if (hangfireOptions.WorkerCount.HasValue && hangfireOptions.WorkerCount.Value > 0)
                {
                    options.WorkerCount = hangfireOptions.WorkerCount.Value;
                }
            });
        }

        context.Services.TryAddTransient<HangFireJobInvoker>();

        // 所有执行交给 Hangfire: 关掉内置 worker, 免得它空轮询进程内存储
        Configure<BackgroundJobOptions>(options => options.IsJobExecutionEnabled = false);

        // 入口替换: 约定注册若已登记其他实现, 这里以 Hangfire 为准
        context.Services.RemoveAll<IBackgroundJobManager>();
        context.Services.AddTransient<IBackgroundJobManager, HangFireBackgroundJobManager>();
        context.Services.RemoveAll<IBackgroundJobScheduler>();
        context.Services.AddSingleton<IBackgroundJobScheduler, HangFireBackgroundJobScheduler>();
    }
}

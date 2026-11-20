using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Snow.BackgroundJobs;
using Snow.DependencyInjection;
using Snow.Messaging;
using Snow.Modularity;
using global::Hangfire;
using global::Hangfire.InMemory;

namespace Snow.BackgroundJobs.HangFire;

/// <summary>
/// 后台任务模块: 注册 Hangfire 服务端与存储, 并把两个作业端口替换成 Hangfire 实现
/// <para>宿主已自行 AddHangfire(配置持久化存储)时不再重复注册, 只接管端口</para>
/// </summary>
[DependsOn(typeof(SnowMessagingModule), typeof(BackgroundJobsModule))]
public sealed class SnowHangFireModule : SnowModule
{
    /// <inheritdoc />
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // 配置节: HangFire:{ EnableServer, EnableDashboard, DashboardPath, UseInMemoryStorage, WorkerCount, PollingIntervalSeconds, TimeZoneId }
        IConfiguration configuration = context.Services.GetConfiguration();
        IConfigurationSection section = configuration.GetSection("HangFire");
        SnowHangFireOptions hangfireOptions = new();
        section.Bind(hangfireOptions);
        Configure<SnowHangFireOptions>(section);

        bool enableServer = hangfireOptions.EnableServer;
        bool useInMemoryStorage = hangfireOptions.UseInMemoryStorage;

        if (!context.Services.Any(descriptor => descriptor.ServiceType == typeof(IGlobalConfiguration)))
        {
            context.Services.AddHangfire(configuration =>
            {
                configuration
                    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                    .UseSimpleAssemblyNameTypeSerializer()
                    .UseRecommendedSerializerSettings();

                if (useInMemoryStorage)
                {
                    configuration.UseInMemoryStorage();
                }
            });
        }

        if (enableServer)
        {
            // 直接写进服务端选项: 走委托而不是 IOptions, 保证服务端构建时拿到的就是这份值
            context.Services.AddHangfireServer(options =>
            {
                // 调度器轮询: 定时任务与重试任务的检查节奏, Hangfire 默认 15 秒, 决定延迟任务多久出队
                options.SchedulePollingInterval = TimeSpan.FromSeconds(Math.Max(1, hangfireOptions.PollingIntervalSeconds));

                if (hangfireOptions.WorkerCount.HasValue && hangfireOptions.WorkerCount.Value > 0)
                {
                    options.WorkerCount = hangfireOptions.WorkerCount.Value;
                }
            });
        }

        // 端口只传类名字符串, 回放时要靠这张表把名字还原成类型
        IModuleContainer moduleContainer = context.Services.GetSingletonInstance<IModuleContainer>();
        context.Services.AddSingleton(new SnowHangFireJobTypeRegistry(moduleContainer));

        // 队列式任务改由 Hangfire 执行: 关掉内置 worker, 免得它空轮询进程内存储
        Configure<BackgroundJobOptions>(options => options.IsJobExecutionEnabled = false);

        // 端口替换: 约定注册若已登记其他实现, 这里以 Hangfire 为准
        context.Services.RemoveAll<IBackgroundJobManager>();
        context.Services.AddTransient<IBackgroundJobManager, HangFireBackgroundJobManager>();
        context.Services.RemoveAll<IBackgroundJobScheduler>();
        context.Services.AddSingleton<IBackgroundJobScheduler, HangFireBackgroundJob>();
        context.Services.RemoveAll<IBackgroundExecutor>();
        context.Services.AddSingleton<IBackgroundExecutor, HangFireBackgroundExecutor>();
    }
}

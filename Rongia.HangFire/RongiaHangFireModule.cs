using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Rongia.Application.Port.Job;
using Rongia.DependencyInjection;
using Rongia.Messaging;
using Rongia.Modularity;
using global::Hangfire;

namespace Rongia.HangFire;

/// <summary>
/// 后台任务模块: 注册 Hangfire 服务端与存储, 并把两个作业端口替换成 Hangfire 实现
/// <para>宿主已自行 AddHangfire(配置持久化存储)时不再重复注册, 只接管端口</para>
/// </summary>
[DependsOn(typeof(RongiaMessagingModule))]
public sealed class RongiaHangFireModule : RongiaModule
{
    /// <inheritdoc />
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // 配置节: HangFire:{ EnableServer, EnableDashboard, DashboardPath, UseInMemoryStorage, WorkerCount, TimeZoneId }
        IConfigurationSection section = context.Configuration.GetSection(RongiaHangFireOptions.SectionName);
        if (section.Exists())
        {
            context.Services.Configure<RongiaHangFireOptions>(section);
        }

        bool enableServer = ReadBoolean(section, nameof(RongiaHangFireOptions.EnableServer), fallback: true);
        bool useInMemoryStorage = ReadBoolean(section, nameof(RongiaHangFireOptions.UseInMemoryStorage), fallback: true);

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
            context.Services.AddHangfireServer();
        }

        // 端口只传类名字符串, 回放时要靠这张表把名字还原成类型
        IModuleContainer moduleContainer = context.Services.GetSingletonInstance<IModuleContainer>();
        context.Services.AddSingleton(new RongiaHangFireJobTypeRegistry(moduleContainer));

        // 端口替换: 约定注册若已登记其他实现, 这里以 Hangfire 为准
        context.Services.RemoveAll<IBackgroundJob>();
        context.Services.AddSingleton<IBackgroundJob, HangFireBackgroundJob>();
        context.Services.RemoveAll<IBackgroundExecutor>();
        context.Services.AddSingleton<IBackgroundExecutor, HangFireBackgroundExecutor>();
    }

    private static bool ReadBoolean(IConfigurationSection section, string key, bool fallback)
    {
        return bool.TryParse(section[key], out bool value) ? value : fallback;
    }
}

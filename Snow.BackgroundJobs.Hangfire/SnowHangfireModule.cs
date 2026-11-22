using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Snow.BackgroundJobs;
using Snow.DependencyInjection;
using Snow.Modularity;
using global::Hangfire;
using global::Hangfire.InMemory;

namespace Snow.BackgroundJobs.Hangfire;

/// <summary>
/// Hangfire 集成模块: 注册 Hangfire 服务端与存储, 并用 Hangfire 实现替换入队与调度入口
/// <para>宿主已自行 AddHangfire(配置持久化存储)时不再重复注册, 只接管入口。</para>
/// <para>入队直接用 lambda, 面板里显示的是真实的服务与方法。</para>
/// </summary>
[DependsOn(typeof(BackgroundJobsModule))]
public sealed class SnowHangfireModule : SnowModule
{
    /// <inheritdoc />
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        IServiceCollection services = context.Services;
        IConfigurationSection section = services.GetConfiguration().GetSection("Hangfire");
        SnowHangfireOptions hangfireOptions = new();
        section.Bind(hangfireOptions);

        Configure<SnowHangfireOptions>(section);

        bool ownsGlobalConfiguration = !services.Any(descriptor => descriptor.ServiceType == typeof(IGlobalConfiguration));

        if (ownsGlobalConfiguration)
        {
            services.AddHangfire(configuration =>
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
            services.AddHangfireServer(options =>
            {
                options.SchedulePollingInterval = TimeSpan.FromSeconds(Math.Max(1, hangfireOptions.PollingIntervalSeconds));

                if (hangfireOptions.WorkerCount.HasValue && hangfireOptions.WorkerCount.Value > 0)
                {
                    options.WorkerCount = hangfireOptions.WorkerCount.Value;
                }
            });
        }

        // 内置 worker 必须让位: 否则同一个任务会被两边各执行一次
        Configure<BackgroundJobOptions>(options => options.Enabled = false);

        // 两个入口的替换由实现类上的 [TransientService(ReplaceServices = true)] /
        // [SingletonService(ReplaceServices = true)] 完成, 约定注册会顶掉默认实现, 这里不写替换代码。
    }
}
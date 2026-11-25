using System;
using global::Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Snow.BackgroundJobs;
using Snow.Modularity;

namespace Snow.BackgroundJobs.Hangfire;

/// <summary>
/// Hangfire 集成模块: 登记 Hangfire(含框架统一的默认设置)与服务端, 并用 Hangfire 实现替换入队与调度入口
/// <para>模块自己做两件事: 给 Hangfire 套上兼容级别与序列化器这些样板(<see cref="CompatibilityLevel.Version_180"/> /
/// <c>UseSimpleAssemblyNameTypeSerializer()</c> / Hangfire 推荐序列化设置), 以及按 <see cref="SnowHangfireOptions"/>
/// 建服务端。存储不在这里定: 宿主在自己的模块 <c>ConfigureServices</c> 里用 Hangfire 的
/// <c>context.Services.AddHangfire(configuration =&gt; configuration.UseSqliteStorage(...))</c> 追加,
/// 后加的配置后生效, 持久化存储自然盖掉默认设置(与 ABP 的 <c>AbpHangfireModule</c> 同一套路)。</para>
/// <para>宿主已自行 <c>AddHangfire</c> 时不必再调本模块的登记, 模块只补设置与服务端; 存储提供程序
/// (Hangfire.SqlServer / Hangfire.PostgreSql / Hangfire.Storage.SQLite 等)由宿主自己引, 本包不绑定任何一种库。</para>
/// <para>入队直接用 lambda, 面板里显示的是真实的服务与方法。</para>
/// <para>仪表盘中间件由宿主自己挂: 在启动模块的 OnApplicationInitialization 里调
/// <c>context.GetApplicationBuilder().UseSnowHangfireDashboard()</c>, 放在授权之后: 面板是终端分支, 早于授权就对匿名请求可见。</para>
/// <para>非 Web 宿主不挂即可, 后台任务本身不受影响。</para>
/// </summary>
[DependsOn(typeof(BackgroundJobsModule))]
public sealed class SnowHangfireModule : SnowModule
{
    private BackgroundJobServer? _server;

    /// <inheritdoc />
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        IServiceCollection services = context.Services;

        // Hangfire 的设置: 数据兼容级别与序列化器这些样板统一在这里给。
        // 宿主之后用 AddHangfire 追加存储, 追加的动作在执行时排在后面, 所以持久化存储会覆盖这里的默认值。
        services.AddHangfire(configuration => configuration
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings());

        // 服务端: 工厂延迟到初始化时才按选项创建, 这样宿主的 Configure<SnowHangfireOptions> 已经生效。
        // 关闭服务端时工厂返回 null, 容器照样解析(得到 null), 只是没有服务端可停。
        services.AddSingleton(provider => CreateServer(provider)!);

        // 内置 worker 必须让位: 否则同一个任务会被两边各执行一次
        services.Configure<BackgroundJobOptions>(options => options.Enabled = false);

        // 两个入口的替换由实现类上的 [TransientService(ReplaceServices = true)] /
        // [SingletonService(ReplaceServices = true)] 完成, 约定注册会顶掉默认实现, 这里不写替换代码。
    }

    /// <inheritdoc />
    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        // 解析即创建: EnableServer 为 false 时工厂返回 null, 服务端不启动
        _server = context.ServiceProvider.GetService<BackgroundJobServer>();
    }

    /// <inheritdoc />
    public override void OnApplicationShutdown(ApplicationShutdownContext context)
    {
        if (_server is null)
        {
            return;
        }

        _server.SendStop();
        _server.Dispose();
    }

    private static BackgroundJobServer? CreateServer(IServiceProvider serviceProvider)
    {
        SnowHangfireOptions options = serviceProvider.GetRequiredService<IOptions<SnowHangfireOptions>>().Value;

        if (!options.EnableServer)
        {
            return null;
        }

        // 宿主给了就直接用那份对象; 没给就用 Hangfire 默认值
        BackgroundJobServerOptions serverOptions = options.ServerOptions ?? new BackgroundJobServerOptions();

        // 作业里的服务由容器解析(与 AddHangfireServer 一样用容器里的激活器), 宿主自己指定了就不动
        serverOptions.Activator ??= serviceProvider.GetRequiredService<JobActivator>();

        return new BackgroundJobServer(serverOptions, serviceProvider.GetRequiredService<JobStorage>(), additionalProcesses: []);
    }
}
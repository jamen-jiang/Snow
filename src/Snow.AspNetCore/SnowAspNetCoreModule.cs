using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Snow.Auditing;
using Snow.Authorization;
using Snow.DependencyInjection;
using Snow.Modularity;
using Snow.Security;
using Snow.Timing;
using Snow.Tracing;
using Snow.Uow;

namespace Snow.AspNetCore;

/// <summary>
/// Web 宿主模块: 做 Web 层接线, 并在应用初始化阶段合并静态资源
/// <para>对齐 ABP 的 <c>AbpAspNetCoreModule</c>: 保证 HTTP 上下文访问器可用,
/// 把只有宿主才有的对象放进对象访问器, 并把各包带的静态资源合并进 WebRoot。</para>
/// <para><b>管道由宿主自己写</b>: 框架不铺骨架、也不映射端点。宿主在 <c>Program.cs</c> 里调一次
/// <c>await app.InitializeApplicationAsync()</c> 驱动模块初始化, 再在启动模块的
/// <c>OnApplicationInitialization</c> 里用 <c>context.GetApplicationBuilder()</c> 按顺序手写
/// <c>app.UseXxx()</c>(链路标识、路由、认证、授权以及各能力包的 UseSnowXxx), 最后调
/// <c>app.UseSnowEndpoints()</c> 映射端点 —— 与 ABP 模板的 <c>await app.InitializeApplicationAsync()</c> 同理。</para>
/// <para>本模块只负责: 上下文访问器、对象访问器、端点选项容器、静态资源合并。</para>
/// <para>依赖 <see cref="SnowSecurityModule"/>: 当前主体以 <c>HttpContext.User</c> 为准, 需要它的上下文访问器。</para>
/// <para>依赖列表参照 ABP 的 <c>AbpAspNetCoreModule</c>: 安全、授权、审计、工作单元在这里。
/// 其余能力模块(缓存 / 分布式锁 / 后台作业 / 事件总线 / 消息 / 邮件 / FTP / HTTP 客户端)只随包提供契约,
/// 由宿主在自己的模块里按需 <c>DependsOn</c> —— 与 ABP 把 Caching / EventBus / BackgroundJobs 留给宿主声明同理。
/// </para>
/// <para>认证模块(<c>SnowAuthenticationModule</c>)故意不在此列: 它需要 JWT 参数, 属于应用层选型, 与 ABP 把 <c>Volo.Abp.AspNetCore.Authentication.JwtBearer</c> 单列同理。</para>
/// </summary>
[DependsOn(
    typeof(SnowAuditingModule),
    typeof(SnowSecurityModule),
    typeof(SnowAuthorizationModule),
    typeof(SnowUowModule),
    typeof(SnowTimingModule),
    typeof(SnowTracingModule)
)]
public sealed class SnowAspNetCoreModule : SnowModule
{
    /// <summary>
    /// 把宿主真实的环境名填进 <see cref="ISnowHostEnvironment"/>
    /// <para>对应 ABP 在 <c>AbpAspNetCoreModule.PreConfigureServices</c> 里做的事: 环境名只有 Web 宿主知道,
    /// 而其它模块在服务注册阶段就要用它判断分支, 所以这里先补上。宿主自己已经填过就不覆盖。</para>
    /// </summary>
    /// <param name="context">服务配置上下文</param>
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        ISnowHostEnvironment hostEnvironment = context.Services.GetSingletonInstance<ISnowHostEnvironment>();
        if (hostEnvironment.EnvironmentName.IsNullOrWhiteSpace())
        {
            hostEnvironment.EnvironmentName = context.Services.GetHostingEnvironment().EnvironmentName;
        }
    }

    /// <inheritdoc />
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // 上下文访问器是 Web 层所有能力的地基(当前用户、当前请求)
        context.Services.AddHttpContextAccessor();

        // 由 InitializeApplicationAsync() 填值, 单例里也能取到当前应用对象
        context.Services.AddObjectAccessor<IApplicationBuilder>();
        context.Services.AddObjectAccessor<WebApplication>();
        context.Services.AddObjectAccessor<IHost>();
        context.Services.AddObjectAccessor<IEndpointRouteBuilder>();

        // 端点清单的选项容器: 模块用 Configure<SnowEndpointRouterOptions>(...) 把映射动作交进来
        context.Services.AddOptions<SnowEndpointRouterOptions>();

        Configure<SnowAspNetCoreOptions>(context.Services.GetConfiguration().GetSection("Snow"));
    }

    /// <inheritdoc />
    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        MergeStaticAssets(context);
    }

    /// <summary>
    /// 把各包带的静态资源合并进 WebRoot
    /// </summary>
    /// <param name="context">应用初始化上下文</param>
    private static void MergeStaticAssets(ApplicationInitializationContext context)
    {
        SnowAspNetCoreOptions options = context
            .ServiceProvider.GetRequiredService<IOptions<SnowAspNetCoreOptions>>()
            .Value;
        if (options.StaticAssetFileProviders.Count == 0)
        {
            return;
        }

        if (!options.MergeStaticAssets)
        {
            // 登记了却被开关静默丢掉等于资源凭空消失, 与管道跳过贡献者一样要在启动日志里留线索
            context
                .ServiceProvider.GetService<ILogger<SnowAspNetCoreModule>>()
                ?.LogWarning(
                    "静态资源合并已关闭(MergeStaticAssets=false): 已登记的 {Count} 个文件提供器全部跳过: {Providers}。"
                        + "若这不是本意, 把 SnowAspNetCoreOptions.MergeStaticAssets 置回 true。",
                    options.StaticAssetFileProviders.Count,
                    string.Join(
                        ", ",
                        options.StaticAssetFileProviders.Select(provider =>
                            provider.GetType().FullName
                        )
                    )
                );
            return;
        }

        IWebHostEnvironment? environment =
            context.ServiceProvider.GetService<IWebHostEnvironment>();
        if (environment is null)
        {
            // 与上面 MergeStaticAssets=false 是两种完全不同的原因, 日志必须分开:
            // 那条是「宿主主动关掉」, 这条是「宿主根本不是 Web 应用, 没有 WebRoot 可并」。
            // 对应 ABP 里 StaticWebAssetsLoader 加载清单失败时那条「清单没加载, 静态资源不可用」的告警。
            context
                .ServiceProvider.GetService<ILogger<SnowAspNetCoreModule>>()
                ?.LogWarning(
                    "静态资源合并跳过: 当前宿主不是 Web 应用(取不到 {Type}), 已登记的 {Count} 个文件提供器不会生效。"
                        + "静态资源清单类告警(构建产物里没有清单)与这里的原因不同, 看日志时别混。",
                    nameof(IWebHostEnvironment),
                    options.StaticAssetFileProviders.Count
                );
            return;
        }

        // 保持原 WebRoot 在最前面, 包内资源只做补充: 同名文件仍以宿主自己的为准
        List<IFileProvider> fileProviders = [environment.WebRootFileProvider];
        fileProviders.AddRange(options.StaticAssetFileProviders);
        environment.WebRootFileProvider = new CompositeFileProvider(fileProviders);
    }
}

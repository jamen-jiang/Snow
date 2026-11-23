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
/// <para><b>管道由宿主自己写</b>: 框架不铺骨架、也不映射端点。宿主在启动模块的
/// <c>OnApplicationInitialization</c> 里用 <c>context.GetApplicationBuilder()</c> 按顺序手写
/// <c>app.UseXxx()</c>(链路标识、路由、认证、授权以及各能力包的 UseSnowXxx), 最后调
/// <c>app.UseSnowEndpoints()</c> 映射端点 —— 与 ABP 模板在宿主模块里手写整条管道同理。</para>
/// <para>本模块只负责: 上下文访问器、对象访问器、端点选项容器、注册启动过滤器(驱动模块生命周期)、静态资源合并。</para>
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
    typeof(SnowTracingModule))]
public sealed class SnowAspNetCoreModule : SnowModule
{
    /// <inheritdoc />
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // 上下文访问器是 Web 层所有能力的地基(当前用户、当前请求)
        context.Services.AddHttpContextAccessor();

        // 启动过滤器填值, 单例里也能取到当前应用对象
        context.Services.AddObjectAccessor<IApplicationBuilder>();
        context.Services.AddObjectAccessor<WebApplication>();
        context.Services.AddObjectAccessor<IHost>();
        context.Services.AddObjectAccessor<IEndpointRouteBuilder>();

        // 端点清单的选项容器: 模块用 Configure<SnowEndpointRouterOptions>(...) 把映射动作交进来
        context.Services.AddOptions<SnowEndpointRouterOptions>();

        // 宿主不用写初始化代码: 应用构建器由过滤器交给访问器, 模块初始化、管道展开与端点映射也由它驱动
        context.Services.AddSingleton<IStartupFilter, SnowAspNetCoreStartupFilter>();

        Configure<SnowAspNetCoreOptions>(context.Services.GetConfiguration().GetSection("Snow"));
    }

    /// <inheritdoc />
    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        MergeStaticAssets(context);
    }

    /// <summary>
    /// 映射端点: <see cref="WebApplication"/> 这类本身就是端点路由构建器的宿主直接映射,
    /// 通用 <see cref="ApplicationBuilder"/> 下走 <c>UseEndpoints</c>
    /// </summary>
    /// <param name="app">应用构建器</param>
    /// <param name="services">根服务提供者</param>
    internal static void MapEndpoints(IApplicationBuilder app, IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(services);

        if (app is IEndpointRouteBuilder endpoints)
        {
            ApplyEndpoints(services, endpoints);
            return;
        }

        app.UseEndpoints(routeBuilder => ApplyEndpoints(services, routeBuilder));
    }

    /// <summary>
    /// 执行各模块登记的端点映射动作
    /// <para>映射端点要的是 <see cref="IEndpointRouteBuilder"/>, 模块在服务注册阶段拿不到它,
    /// 所以模块只把动作交进 <see cref="SnowEndpointRouterOptions"/>, 由框架在管道最后一步统一执行。</para>
    /// </summary>
    /// <param name="services">根服务提供者</param>
    /// <param name="endpoints">端点路由构建器</param>
    internal static void ApplyEndpoints(IServiceProvider services, IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(endpoints);

        SnowEndpointRouterOptions options = services.GetRequiredService<IOptions<SnowEndpointRouterOptions>>().Value;
        if (options.EndpointConfigureActions.Count == 0)
        {
            return;
        }

        // 独立作用域: 映射动作里解析 Scoped 服务不会捕获根容器
        using IServiceScope scope = endpoints.ServiceProvider.CreateScope();
        SnowEndpointRouteBuilderContext endpointContext = new(endpoints, scope.ServiceProvider);

        foreach (Action<SnowEndpointRouteBuilderContext> configure in options.EndpointConfigureActions)
        {
            configure(endpointContext);
        }
    }

    /// <summary>
    /// 把各包带的静态资源合并进 WebRoot
    /// </summary>
    /// <param name="context">应用初始化上下文</param>
    private static void MergeStaticAssets(ApplicationInitializationContext context)
    {
        SnowAspNetCoreOptions options = context.ServiceProvider.GetRequiredService<IOptions<SnowAspNetCoreOptions>>().Value;
        if (options.StaticAssetFileProviders.Count == 0)
        {
            return;
        }

        if (!options.MergeStaticAssets)
        {
            // 登记了却被开关静默丢掉等于资源凭空消失, 与管道跳过贡献者一样要在启动日志里留线索
            context.ServiceProvider.GetService<ILogger<SnowAspNetCoreModule>>()?.LogWarning(
                "静态资源合并已关闭(MergeStaticAssets=false): 已登记的 {Count} 个文件提供器全部跳过: {Providers}。"
                + "若这不是本意, 把 SnowAspNetCoreOptions.MergeStaticAssets 置回 true。",
                options.StaticAssetFileProviders.Count,
                string.Join(", ", options.StaticAssetFileProviders.Select(provider => provider.GetType().FullName)));
            return;
        }

        IWebHostEnvironment? environment = context.ServiceProvider.GetService<IWebHostEnvironment>();
        if (environment is null)
        {
            return;
        }

        // 保持原 WebRoot 在最前面, 包内资源只做补充: 同名文件仍以宿主自己的为准
        List<IFileProvider> fileProviders = [environment.WebRootFileProvider];
        fileProviders.AddRange(options.StaticAssetFileProviders);
        environment.WebRootFileProvider = new CompositeFileProvider(fileProviders);
    }
}

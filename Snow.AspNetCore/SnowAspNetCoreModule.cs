using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Snow.Aop.Castle;
using Snow.Auditing;
using Snow.Authorization;
using Snow.Data;
using Snow.DependencyInjection;
using Snow.Modularity;
using Snow.Security;
using Snow.Timing;
using Snow.Tracing;
using Snow.Uow;

namespace Snow.AspNetCore;

/// <summary>
/// Web 宿主模块: 做 Web 层接线, 并在应用初始化阶段合并静态资源、展开 HTTP 管道
/// <para>对齐 ABP 的 <c>AbpAspNetCoreModule</c>: 保证 HTTP 上下文访问器可用,
/// 把只有宿主才有的对象放进对象访问器, 并把各包带的静态资源合并进 WebRoot。</para>
/// <para><b>管道由框架展开</b>: 骨架写死在 <see cref="SnowApplicationBuilder"/> 里
/// (链路标识 → 路由 → 认证 → 授权 → 端点), 各能力包只把自己的中间件登记成
/// <see cref="ISnowPipelineFilter"/> 挂到某个具名位置上, 宿主一行管道代码都不用写。</para>
/// <para><b>宿主不用写初始化代码</b>: 模块初始化、管道展开与端点映射都由本模块注册的
/// <see cref="SnowAspNetCoreStartupFilter"/> 自动触发。</para>
/// <para>依赖 <see cref="SnowSecurityModule"/>: 当前主体以 <c>HttpContext.User</c> 为准, 需要它的上下文访问器。</para>
/// <para>依赖列表对齐 ABP 的 <c>AbpAspNetCoreModule</c>(它依赖 Auditing / Security / VirtualFileSystem / UnitOfWork / Http / Authorization / Validation / ExceptionHandling / AspNetCoreAbstractions):
/// 安全、授权、审计、工作单元、数据过滤在这里; 虚拟文件系统 / 校验 / 异常包装 Snow 尚无独立包。
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
    typeof(SnowDataModule),
    typeof(SnowAopModule),
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

        // 管道过滤器清单: 各能力包用 Configure<SnowPipelineOptions>(options => options.AddFilter(...)) 登记
        context.Services.AddOptions<SnowPipelineOptions>();

        // 宿主不用写初始化代码: 应用构建器由过滤器交给访问器, 模块初始化、管道展开与端点映射也由它驱动
        context.Services.AddSingleton<IStartupFilter, SnowAspNetCoreStartupFilter>();

        Configure<SnowAspNetCoreOptions>(context.Services.GetConfiguration().GetSection("AspNetCore"));
    }

    /// <inheritdoc />
    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        MergeStaticAssets(context);
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
        if (!options.MergeStaticAssets || options.StaticAssetFileProviders.Count == 0)
        {
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

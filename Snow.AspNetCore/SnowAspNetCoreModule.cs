using Microsoft.Extensions.DependencyInjection.Extensions;
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
/// Web 宿主模块: 只做 Web 层接线, 不含 MVC 约定
/// <para>对齐 ABP 的 <c>AbpAspNetCoreModule</c>: 保证 HTTP 上下文访问器可用,
/// 把只有宿主才有的对象放进对象访问器, 并把各包带的静态资源合并进 WebRoot。</para>
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

        // 宿主构建完成后由 UseSnowAspNetCore / InitializeSnowAsync 填值, 单例里也能取到当前应用对象
        context.Services.AddObjectAccessor<IApplicationBuilder>();
        context.Services.AddObjectAccessor<WebApplication>();
        context.Services.AddObjectAccessor<IHost>();
        context.Services.AddObjectAccessor<IEndpointRouteBuilder>();

        Configure<SnowAspNetCoreOptions>(context.Services.GetConfiguration().GetSection("AspNetCore"));

        // 中间件管道: 各包把自己要挂的中间件登记进来, 宿主在锚点用 UseSnowPipeline(阶段) 一次展开
        context.Services.TryAddSingleton<SnowMiddlewareOptionsSetup>();
        context.Services.TryAddSingleton<SnowPipelineState>();
        context.Services.TryAddSingleton<IOptions<SnowMiddlewareOptions>>(serviceProvider =>
        {
            SnowMiddlewareOptions pipelineOptions = new();
            serviceProvider.GetRequiredService<SnowMiddlewareOptionsSetup>().Apply(pipelineOptions);
            return Options.Create(pipelineOptions);
        });

    }

    /// <inheritdoc />
    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // 链路标识是最外层中间件: 在应用初始化阶段由本包自己挂上(宿主只需最早调用 UseSnowAspNetCore/InitializeSnowAsync)
        context.GetApplicationBuilderOrNull()?.UseSnowMiddlewareOnce<SnowCorrelationIdMiddleware>();

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
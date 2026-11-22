using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
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
/// <para>本模块还是管道的唯一装配点: 各模块用 <c>AddSnowMiddleware&lt;T&gt;(stage)</c> 声明中间件,
/// 用 <c>AddSnowEndpoint(...)</c> 声明端点, 这里按阶段顺序展开。路由 / 认证 / 授权三段由本模块自己插在阶段之间,
/// 所以模块只表达"在哪一段", 不碰任何顺序数字, 宿主也不写管道代码。</para>
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

        // 管道登记表必须存在: 即使没有任何模块登记, 本模块也要靠它展开路由 / 认证 / 授权
        context.Services.GetSnowPipelineRegistry();

        // 链路标识与位置无关, 挂最外层让后续所有日志与审计都能读到
        context.Services.AddSnowMiddleware<SnowCorrelationIdMiddleware>(SnowPipelineStage.BeforeRouting);
    }

    /// <inheritdoc />
    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.GetApplicationBuilderOrNull() is { } app)
        {
            ExpandPipeline(app, context.ServiceProvider.GetRequiredService<SnowPipelineRegistry>());

            // 各模块登记的端点动作(Controller / Hub / 健康检查 / 接口文档)也在这里一次挂完, 宿主不用再写 UseSnowEndpoints()
            app.UseSnowEndpointsIfWebHost();
        }

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

    /// <summary>
    /// 按阶段展开管道: 每个阶段先跑模块登记的中间件, 再插入该阶段之后由框架负责的那一段
    /// </summary>
    /// <param name="app">应用构建器</param>
    /// <param name="registry">管道登记表</param>
    private static void ExpandPipeline(IApplicationBuilder app, SnowPipelineRegistry registry)
    {
        // 1. 最外层: 兜异常用, 要在一切之前
        ApplyStage(app, registry, SnowPipelineStage.Outermost);

        // 2. 路由之前: 链路标识、日志增强
        ApplyStage(app, registry, SnowPipelineStage.BeforeRouting);

        // 3. 路由: 没有注册路由服务(极简宿主)时跳过, UseRouting 会因缺少 EndpointDataSource 报错
        if (app.ApplicationServices.GetService<EndpointDataSource>() is not null)
        {
            app.UseRouting();
        }

        // 4. 路由之后、认证之前
        ApplyStage(app, registry, SnowPipelineStage.AfterRouting);

        // 5. 认证: 宿主一个方案都没登记时跳过, 少了 AuthenticationMiddleware 也不影响匿名端点
        if (app.ApplicationServices.GetService<IAuthenticationSchemeProvider>() is not null)
        {
            app.UseAuthentication();
        }

        // 6. 认证之后、授权之前: 多租户要读已认证用户与路由值
        ApplyStage(app, registry, SnowPipelineStage.AfterAuthentication);

        // 7. 授权
        if (app.ApplicationServices.GetService<IAuthorizationPolicyProvider>() is not null)
        {
            app.UseAuthorization();
        }

        // 8. 授权之后、端点之前: 限流要读端点上的策略
        ApplyStage(app, registry, SnowPipelineStage.AfterAuthorization);

        // 9. 端点之前的终端分支: 仪表盘、接口文档
        ApplyStage(app, registry, SnowPipelineStage.BeforeEndpoints);
    }

    private static void ApplyStage(IApplicationBuilder app, SnowPipelineRegistry registry, SnowPipelineStage stage)
    {
        foreach (SnowPipelineEntry entry in registry.MiddlewareIn(stage))
        {
            entry.Configure(app);
        }
    }
}

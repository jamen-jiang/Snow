using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Snow.AspNetCore.Authorization;
using Snow.AspNetCore.ExceptionHandling;
using Snow.Authorization;
using Snow.DependencyInjection;
using Snow.ExceptionHandling;
using Snow.Modularity;
using Snow.Security;
using Snow.Tracing;
using Snow.Uow;
using Snow.Validation;

namespace Snow.AspNetCore;

/// <summary>
/// Web 宿主模块: 做 Web 层接线(上下文访问器、对象访问器、选项容器)
/// <para>对齐 ABP 的 <c>AbpAspNetCoreModule</c>: 保证 HTTP 上下文访问器可用,
/// 把只有宿主才有的对象放进对象访问器, 并把各包带的静态资源合并进 WebRoot。</para>
/// <para><b>管道由宿主自己写</b>: 框架不铺骨架、也不映射端点。宿主在 <c>Program.cs</c> 里调一次
/// <c>await app.InitializeApplicationAsync()</c> 驱动模块初始化, 再在启动模块的
/// <c>OnApplicationInitialization</c> 里用 <c>context.GetApplicationBuilder()</c> 按顺序手写
/// <c>app.UseXxx()</c>(链路标识、路由、认证、授权以及各能力包的 UseSnowXxx), 最后调
/// <c>app.UseSnowEndpoints()</c> 映射端点 —— 与 ABP 模板的 <c>await app.InitializeApplicationAsync()</c> 同理。</para>
/// <para>本模块只负责: 上下文访问器、对象访问器、端点选项容器与异常状态码选项容器。</para>
/// <para>依赖 <see cref="SnowSecurityModule"/>: 当前主体以 <c>HttpContext.User</c> 为准, 本包提供 HTTP 上下文访问器与 Web 端主体实现 <c>HttpContextCurrentPrincipalAccessor</c>。</para>
/// <para>依赖列表参照 ABP 的 <c>AbpAspNetCoreModule</c>: 安全、授权、工作单元在这里。
/// 其余能力模块(缓存 / 分布式锁 / 后台作业 / 事件总线 / 消息 / 邮件 / FTP / HTTP 客户端)只随包提供契约,
/// 由宿主在自己的模块里按需 <c>DependsOn</c> —— 与 ABP 把 Caching / EventBus / BackgroundJobs 留给宿主声明同理。
/// </para>
/// <para>认证模块(<c>SnowAuthenticationModule</c>)故意不在此列: 它需要 JWT 参数, 属于应用层选型, 与 ABP 把 <c>Volo.Abp.AspNetCore.Authentication.JwtBearer</c> 单列同理。</para>
/// <para><b>与 ABP 的依赖差异</b>: ABP 的 <c>AbpAspNetCoreModule</c> 未在 <c>[DependsOn]</c> 里直接声明 Timing/Tracing。Snow 此处也不声明 <c>SnowTimingModule</c> —— 本包代码不使用任何时间抽象, 需要时间的宿主自己声明; <c>SnowTracingModule</c> 则保留显式声明, 因为本包的 <c>SnowCorrelationIdMiddleware</c> 直接使用 <c>Snow.Tracing.ICorrelationIdProvider</c>, 显式声明才能让 <c>UseSnowCorrelationId()</c> 开箱可用。</para>
/// </summary>
[DependsOn(
    typeof(SnowSecurityModule),
    typeof(SnowAuthorizationModule),
    typeof(SnowUowModule),
    typeof(SnowTracingModule),
    typeof(SnowExceptionHandlingModule),
    typeof(SnowValidationModule)
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
    /// <param name="context">上下文</param>
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // 上下文访问器是 Web 层所有能力的地基(当前用户、当前请求)
        context.Services.AddHttpContextAccessor();

        // 授权服务: 策略提供器/处理器由 Snow.Authorization 提供, 但 AddAuthorization 属于 ASP.NET Core 集成件
        // (对齐 ABP: 该调用在 AbpAspNetCoreModule, 不在 Volo.Abp.Authorization)
        context.Services.AddAuthorization();

        // 由 InitializeApplicationAsync() 填值, 单例里也能取到当前应用对象
        context.Services.AddObjectAccessor<IApplicationBuilder>();
        context.Services.AddObjectAccessor<WebApplication>();
        context.Services.AddObjectAccessor<IHost>();
        context.Services.AddObjectAccessor<IEndpointRouteBuilder>();

        // 端点清单的选项容器: 模块用 Configure<SnowEndpointRouterOptions>(...) 把映射动作交进来
        context.Services.AddOptions<SnowEndpointRouterOptions>();

        // 异常转换器 / SnowExceptionHandlingOptions 及其本地化资源统一由 SnowExceptionHandlingModule 登记(见 DependsOn);
        // 状态码解析器与授权异常处理器由 [TransientService] 注解约定注册, 这里只登记状态码选项对象
        context.Services.AddOptions<SnowExceptionHttpStatusCodeOptions>();
    }
}

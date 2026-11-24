using Snow.Aop.Castle;
using Snow.AspNetCore;
using Snow.AspNetCore.MultiTenancy;
using Snow.AspNetCore.Mvc;
using Snow.AspNetCore.RateLimiting;
using Snow.AspNetCore.Serilog;
using Snow.AspNetCore.SignalR;
using Snow.Authentication;
using Snow.Authorization;
using Snow.BackgroundJobs;
using Snow.BackgroundJobs.Hangfire;
using Snow.Caching.StackExchangeRedis;
using Snow.Data;
using Snow.DistributedLocking;
using Snow.Emailing.MailKit;
using Snow.EntityFrameworkCore;
using Snow.EventBus;
using Snow.Ftping.FluentFTP;
using Snow.Http.Client;
using Snow.Json;
using Snow.Json.Newtonsoft;
using Snow.Localization;
using Snow.Messaging;
using Snow.Modularity;
using Snow.MultiTenancy;
using Snow.ObjectMapping.Mapster;
using Snow.OpenApi;
using Snow.Security;
using Snow.Timing;
using Snow.Tracing;
using Snow.Uow;
using Snow.Validation.FluentValidation;
using Snow.Workflows.Elsa;

namespace Snow.Sample.Web;

/// <summary>
/// 全模块示例启动模块: 一次性 <c>DependsOn</c> 框架全部能力模块与各实现提供者。
/// <para>按 Snow 约定, 管道由启动模块在 <see cref="OnApplicationInitialization"/> 里手写。</para>
/// </summary>
[DependsOn(
    typeof(SnowAopModule),
    typeof(SnowAspNetCoreModule),
    typeof(SnowAspNetCoreMultiTenancyModule),
    typeof(SnowAspNetCoreMvcModule),
    typeof(SnowRateLimitingModule),
    typeof(SnowAspNetCoreSerilogModule),
    typeof(SnowSignalRModule),
    typeof(SnowAuthenticationModule),
    typeof(SnowAuthorizationModule),
    typeof(BackgroundJobsModule),
    typeof(SnowHangfireModule),
    typeof(SnowCachingStackExchangeRedisModule),
    typeof(SnowDataModule),
    typeof(SnowDistributedLockRedisModule),
    typeof(SnowEmailingMailKitModule),
    typeof(SnowEntityFrameworkCoreModule),
    typeof(SnowEntityFrameworkCoreMySqlModule),
    typeof(SnowEntityFrameworkCorePostgreSqlModule),
    typeof(SnowEntityFrameworkCoreSqlServerModule),
    typeof(SnowEntityFrameworkCoreSqliteModule),
    typeof(SnowEventBusModule),
    typeof(SnowFtpingFluentFTPModule),
    typeof(SnowHttpClientModule),
    typeof(SnowJsonModule),
    typeof(SnowJsonNewtonsoftModule),
    typeof(SnowLocalizationModule),
    typeof(SnowMessagingModule),
    typeof(SnowMultiTenancyModule),
    typeof(SnowObjectMappingMapsterModule),
    typeof(SnowOpenApiModule),
    typeof(SnowSecurityModule),
    typeof(SnowTimingModule),
    typeof(SnowTracingModule),
    typeof(SnowUowModule),
    typeof(SnowValidationFluentValidationModule),
    typeof(SnowElsaModule)
)]
public sealed class SampleModule : SnowModule
{
    /// <inheritdoc />
    /// <param name="context">上下文</param>
    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Snow 约定: 模块不铺管道, 整条管道由启动模块在这里按行序手写(对齐 ABP 模板)。
        // 下面把框架各能力包提供的 UseSnowXxx 逐一接上, 书写顺序即中间件的嵌套顺序(先挂的在最外层)。
        IApplicationBuilder app = context.GetApplicationBuilder();

        // 0) 异常处理: 最外层, 把后续管道抛出的所有异常翻译成统一错误信封(JSON)
        app.UseSnowExceptionHandling();

        // 1) 链路标识: 必须最外层 —— 认证失败 / 授权拒绝 / 路由没匹配(404) 的响应也要带回 X-Correlation-Id
        app.UseSnowCorrelationId();

        // 2) 请求级日志上下文增强: 尽量靠外, 让后续中间件与业务日志都带上 用户 / 租户 / 链路;
        //    要能读到链路标识, 所以排在 UseSnowCorrelationId 之内
        app.UseSnowSerilogEnrichers();

        // 3) 登录态异常转 401/403: 必须在 UseAuthentication() 之前, 才能包住 JwtBearer 认证阶段抛出的令牌异常
        app.UseSnowTokenExceptionHandling();

        // 4) 路由: 后续限流与端点映射都依赖它先把 endpoint 解析出来
        app.UseRouting();

        // 5) 限流: 必须在 UseRouting() 之后 —— 限流策略挂在端点元数据上, 路由之前读不到会静默失效
        app.UseSnowRateLimiting();

        // 6) 认证
        app.UseAuthentication();

        // 7) 多租户解析: 必须在 UseAuthentication() 之后 —— 路由出资者要读路由值, 当前用户出资者要读已认证主体;
        //    未启用多租户时中间件原样放行, 零开销
        app.UseSnowMultiTenancy();

        // 8) 授权
        app.UseAuthorization();

        // 9) 后台作业仪表盘(/hangfire): 授权之后、端点之前 —— 终端分支, 早于授权就对匿名请求可见
        app.UseSnowHangfireDashboard();

        // 10) 工作流 HTTP 端点(/workflows): 同上, 终端分支, 落点必须在授权之后
        app.UseSnowWorkflows();

        // 11) 端点映射: 管道最后一步, 统一展开各模块登记的端点(含 OpenAPI / Scalar 文档)
        app.UseSnowEndpoints();
    }
}

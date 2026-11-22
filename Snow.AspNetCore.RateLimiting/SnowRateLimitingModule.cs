using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Snow.Modularity;
using Snow.AspNetCore;

namespace Snow.AspNetCore.RateLimiting;

/// <summary>
/// 限流模块: 调用微软自带的 AddRateLimiter
/// <para>依赖 <see cref="SnowAspNetCoreModule"/>: 限流中间件挂在 HTTP 管道上。</para>
/// <para>本模块不定义任何自有选项: 全局限流器与命名策略都由宿主用 <see cref="RateLimiterOptions"/> 原样表达。</para>
/// </summary>
[DependsOn(typeof(SnowAspNetCoreModule))]
public sealed class SnowRateLimitingModule : SnowModule
{
    /// <inheritdoc />
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        SnowRateLimitingOptionsSetup? setup = context.Services
            .Where(descriptor => descriptor.ServiceType == typeof(SnowRateLimitingOptionsSetup))
            .Select(descriptor => descriptor.ImplementationInstance)
            .OfType<SnowRateLimitingOptionsSetup>()
            .FirstOrDefault();

        // 即使宿主没有追加任何配置也登记一次, 使 UseRateLimiter 与 [EnableRateLimiting] 可用
        context.Services.AddRateLimiter(limiter => setup?.Apply(limiter));

        // 中间件由本模块自己挂: 排在路由与授权之后(要读端点上的限流策略)、端点之前。
        // 微软的 RateLimitingMiddleware 是 internal, 只能用 UseRateLimiter() 这个公开扩展
        context.Services.AddSnowMiddleware(SnowPipelineStage.AfterAuthorization, app => app.UseRateLimiter());
    }
}
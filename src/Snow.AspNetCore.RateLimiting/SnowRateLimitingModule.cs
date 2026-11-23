using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Snow.AspNetCore;
using Snow.Modularity;

namespace Snow.AspNetCore.RateLimiting;

/// <summary>
/// 限流模块: 调用微软自带的 AddRateLimiter
/// <para>依赖 <see cref="SnowAspNetCoreModule"/>: 限流中间件挂在 HTTP 管道上。</para>
/// <para>本模块不定义任何自有选项: 全局限流器与命名策略都由宿主用 <see cref="RateLimiterOptions"/> 原样表达。</para>
/// <para>限流中间件由宿主自己挂: 在启动模块的 OnApplicationInitialization 里调
/// <c>context.GetApplicationBuilder().UseSnowRateLimiting()</c>, 必须放在 <c>UseRouting()</c> 之后 ——
/// 限流策略挂在端点元数据上, 路由之前读不到, 会表现为"限流全部失效且不报错"。</para>
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
    }

}

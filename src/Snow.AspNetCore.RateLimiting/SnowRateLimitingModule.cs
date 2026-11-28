using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Snow.AspNetCore;
using Snow.Modularity;

namespace Snow.AspNetCore.RateLimiting;

/// <summary>
/// 限流模块: 调用微软自带的 AddRateLimiter
/// <para>依赖 <see cref="SnowAspNetCoreModule"/>: 限流中间件挂在 HTTP 管道上。</para>
/// <para>限流配置由宿主在 <c>PreConfigureServices</c> 里 <c>Configure&lt;SnowRateLimitingOptions&gt;(o =&gt; o.Configure(...))</c> 提供,
/// 动作收到微软原生的 <see cref="RateLimiterOptions"/>, 可多次追加。</para>
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

        context.Services.AddOptions<SnowRateLimitingOptions>();

        // 选项由宿主在 PreConfigureServices 里 Configure<SnowRateLimitingOptions>(...) 登记;
        // 这里取结算快照, 把聚合后的配置动作交给 AddRateLimiter。
        SnowRateLimitingOptions options = context.Services.GetOptions<SnowRateLimitingOptions>();

        context.Services.AddRateLimiter(options.Apply);
    }
}

using Microsoft.AspNetCore.Builder;
using Snow.AspNetCore;
using Snow.Modularity;
using Snow.MultiTenancy;
using Snow.Security;
using Snow.Tracing;

namespace Snow.AspNetCore.Serilog;

/// <summary>
/// 日志模块: 装配 Serilog 的日志上下文增强
/// <para>日志增强中间件由本模块登记成管道过滤器, 挂在 <c>OnPrePipeline</c>(骨架最外层): 它要把每一次请求都包进日志上下文,
/// 包括被链路标识、认证、授权拒绝的请求, 所以位置必须比骨架里的任何一层都外。</para>
/// <para>依赖 <see cref="SnowMultiTenancyModule"/>: 中间件把当前租户标识压进日志上下文(对齐 ABP 的 <c>AbpAspNetCoreSerilogModule</c> 依赖 <c>AbpMultiTenancyModule</c>)。</para>
/// <para>依赖 <see cref="SnowTracingModule"/>: 中间件把当前链路标识压进日志上下文。</para>
/// <para>依赖 <see cref="SnowAspNetCoreModule"/>: 与 ABP 的 <c>AbpAspNetCoreSerilogModule</c> 一样挂在基础 Web 集成层之上(它依赖 <c>AbpAspNetCoreModule</c>)。</para>
/// </summary>
[DependsOn(
    typeof(SnowMultiTenancyModule),
    typeof(SnowAspNetCoreModule),
    typeof(SnowSecurityModule),
    typeof(SnowTracingModule))]
public sealed class SnowAspNetCoreSerilogModule : SnowModule
{
    /// <inheritdoc />
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        Configure<SnowPipelineOptions>(options => options.AddFilter(new SnowPipelineFilter(
            "Snow.AspNetCore.Serilog",
            prePipeline: app => app.UseSnowSerilogEnrichers())));
    }
}

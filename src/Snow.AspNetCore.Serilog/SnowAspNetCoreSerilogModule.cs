using Microsoft.AspNetCore.Builder;
using Snow.AspNetCore;
using Snow.Modularity;
using Snow.Security;
using Snow.Tracing;

namespace Snow.AspNetCore.Serilog;

/// <summary>
/// 日志模块: 装配 Serilog 的日志上下文增强
/// <para>日志增强中间件由宿主自己挂: 在启动模块的 OnApplicationInitialization 里调
/// <c>context.GetApplicationBuilder().UseSnowSerilogEnrichers()</c>, 放在整条管道最外层 ——
/// 它要把每一次请求都包进日志上下文, 包括被令牌校验、认证、授权拒绝的请求。</para>
/// <para>依赖 <see cref="SnowTracingModule"/>: 中间件把当前链路标识压进日志上下文。</para>
/// <para>依赖 <see cref="SnowAspNetCoreModule"/>: 与 ABP 的 <c>AbpAspNetCoreSerilogModule</c> 一样挂在基础 Web 集成层之上(它依赖 <c>AbpAspNetCoreModule</c>)。</para>
/// </summary>
[DependsOn(
    typeof(SnowAspNetCoreModule),
    typeof(SnowSecurityModule),
    typeof(SnowTracingModule))]
public sealed class SnowAspNetCoreSerilogModule : SnowModule
{
}

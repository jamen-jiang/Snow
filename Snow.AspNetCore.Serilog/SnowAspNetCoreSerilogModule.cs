using Snow.Modularity;
using Snow.Security;

namespace Snow.AspNetCore.Serilog;

/// <summary>
/// 日志模块: 装配 Serilog 的日志上下文增强
/// <para>对齐 ABP 的 <c>AbpAspNetCoreSerilogModule</c>; 中间件由 <c>UseSnowSerilogEnrichers()</c> 挂到请求管道。</para>
/// </summary>
[DependsOn(typeof(SnowSecurityModule))]
public sealed class SnowAspNetCoreSerilogModule : SnowModule
{
}

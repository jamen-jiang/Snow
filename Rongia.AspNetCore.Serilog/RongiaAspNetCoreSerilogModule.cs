using Rongia.Modularity;
using Rongia.Security;

namespace Rongia.AspNetCore.Serilog;

/// <summary>
/// 日志模块: 装配 Serilog 的日志上下文增强
/// <para>对齐 ABP 的 <c>AbpAspNetCoreSerilogModule</c>; 中间件由 <c>UseRongiaSerilogEnrichers()</c> 挂到请求管道。</para>
/// </summary>
[DependsOn(typeof(RongiaSecurityModule))]
public sealed class RongiaAspNetCoreSerilogModule : RongiaModule
{
}

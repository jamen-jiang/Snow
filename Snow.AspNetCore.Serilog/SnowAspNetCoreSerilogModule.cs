using Snow.Modularity;
using Snow.MultiTenancy;
using Snow.Security;
using Snow.Tracing;

namespace Snow.AspNetCore.Serilog;

/// <summary>
/// 日志模块: 装配 Serilog 的日志上下文增强
/// <para>对齐 ABP 的 <c>AbpAspNetCoreSerilogModule</c>; 中间件由本模块在应用初始化阶段自动挂到管道最前面(宿主无需手写 <c>UseSnowSerilogEnrichers()</c>)。</para>
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
    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // 日志上下文增强是最外层中间件: 在应用初始化阶段由本模块自己挂上
        // 顺序由模块依赖保证: 链路标识(SnowAspNetCoreModule)先挂, 日志增强后挂
        context.GetApplicationBuilderOrNull()?.UseSnowMiddlewareOnce<SnowSerilogMiddleware>();
    }
}
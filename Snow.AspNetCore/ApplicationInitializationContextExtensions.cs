using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Snow.Modularity;

namespace Snow.AspNetCore;

/// <summary>
/// 应用初始化上下文的宿主对象扩展(对应 ABP 的 <c>ApplicationInitializationContextExtensions</c>)
/// <para>这些扩展都依赖 ASP.NET Core 类型, 所以放在 Snow.AspNetCore 而不是 Snow.Core ——
/// 与 ABP 把它们放在 Volo.Abp.AspNetCore 同理; 控制台/Worker 宿主不必被迫拉入 Web 框架。</para>
/// </summary>
public static class ApplicationInitializationContextExtensions
{
    /// <summary>
    /// 取应用构建器: 模块在 <c>OnApplicationInitialization</c> 里挂中间件时用它
    /// </summary>
    /// <param name="context">应用初始化上下文</param>
    /// <returns>应用构建器</returns>
    /// <exception cref="SnowInitializationException">宿主还没把应用构建器放进对象访问器</exception>
    public static IApplicationBuilder GetApplicationBuilder(this ApplicationInitializationContext context)
    {
        return context.GetApplicationBuilderOrNull() ?? throw new SnowInitializationException(
            "应用构建器尚未就绪: 启动模块没有依赖 SnowAspNetCoreModule, 它的启动过滤器因此没有注册。");
    }

    /// <summary>
    /// 取应用构建器, 取不到返回 null(非 Web 宿主)
    /// </summary>
    /// <param name="context">应用初始化上下文</param>
    /// <returns>应用构建器或 null</returns>
    public static IApplicationBuilder? GetApplicationBuilderOrNull(this ApplicationInitializationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.ServiceProvider.GetService<IObjectAccessor<IApplicationBuilder>>()?.Value;
    }

    /// <summary>
    /// 取宿主
    /// </summary>
    /// <param name="context">应用初始化上下文</param>
    /// <returns>宿主</returns>
    public static IHost GetHost(this ApplicationInitializationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.ServiceProvider.GetService<IObjectAccessor<IHost>>()?.Value
            ?? context.ServiceProvider.GetRequiredService<IHost>();
    }

    /// <summary>
    /// 取 WebApplication(通用主机会返回 null)
    /// </summary>
    /// <param name="context">应用初始化上下文</param>
    /// <returns>Web 应用或 null</returns>
    public static WebApplication? GetWebApplication(this ApplicationInitializationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.ServiceProvider.GetService<IObjectAccessor<WebApplication>>()?.Value;
    }

    /// <summary>
    /// 取端点路由构建器
    /// </summary>
    /// <param name="context">应用初始化上下文</param>
    /// <returns>端点路由构建器或 null</returns>
    public static IEndpointRouteBuilder? GetEndpointRouteBuilder(this ApplicationInitializationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.ServiceProvider.GetService<IObjectAccessor<IEndpointRouteBuilder>>()?.Value;
    }

    /// <summary>
    /// 取 Web 宿主环境
    /// </summary>
    /// <param name="context">应用初始化上下文</param>
    /// <returns>Web 宿主环境或 null</returns>
    public static IWebHostEnvironment? GetEnvironment(this ApplicationInitializationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.ServiceProvider.GetService<IWebHostEnvironment>();
    }

    /// <summary>
    /// 取配置
    /// </summary>
    /// <param name="context">应用初始化上下文</param>
    /// <returns>配置</returns>
    public static IConfiguration GetConfiguration(this ApplicationInitializationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.ServiceProvider.GetRequiredService<IConfiguration>();
    }
}

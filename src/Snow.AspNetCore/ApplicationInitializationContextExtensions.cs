using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Snow.DependencyInjection;
using Snow.Modularity;

namespace Snow.AspNetCore;

/// <summary>
/// 应用初始化上下文的宿主对象扩展(对应 ABP 的 <c>ApplicationInitializationContextExtensions</c>)
/// <para>这些扩展都依赖 ASP.NET Core 类型, 所以放在 Snow.AspNetCore 而不是 Snow.Core ——
/// 与 ABP 把它们放在 Volo.Abp.AspNetCore 同理; 控制台/Worker 宿主不必被迫拉入 Web 框架。</para>
/// <para>每个取宿主对象的扩展都有两个版本: 带 <c>OrNull</c> 的取不到返回 null, 不带的取不到抛
/// <see cref="SnowInitializationException"/>, 便于模块在非 Web 宿主里用 <c>OrNull</c> 版本先判断。</para>
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
        ArgumentNullException.ThrowIfNull(context);

        return context.GetApplicationBuilderOrNull() ?? throw new SnowInitializationException(
            "应用构建器尚未就绪: 宿主还没有调用 app.InitializeApplicationAsync(), 或启动模块没有依赖 SnowAspNetCoreModule。");
    }

    /// <summary>
    /// 取应用构建器, 取不到返回 null(非 Web 宿主)
    /// </summary>
    /// <param name="context">应用初始化上下文</param>
    /// <returns>应用构建器或 null</returns>
    public static IApplicationBuilder? GetApplicationBuilderOrNull(this ApplicationInitializationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.ServiceProvider.GetObjectOrNull<IApplicationBuilder>();
    }

    /// <summary>
    /// 取宿主
    /// </summary>
    /// <param name="context">应用初始化上下文</param>
    /// <returns>宿主</returns>
    /// <exception cref="SnowInitializationException">容器里也没有 IHost</exception>
    public static IHost GetHost(this ApplicationInitializationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.GetHostOrNull() ?? throw new SnowInitializationException(
            "宿主尚未就绪: 容器里没有 IHost, 宿主也没有通过对象访问器放进一个。");
    }

    /// <summary>
    /// 取宿主, 取不到返回 null
    /// </summary>
    /// <param name="context">应用初始化上下文</param>
    /// <returns>宿主或 null</returns>
    public static IHost? GetHostOrNull(this ApplicationInitializationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.ServiceProvider.GetObjectOrNull<IHost>()
            ?? context.ServiceProvider.GetService<IHost>();
    }

    /// <summary>
    /// 取端点路由构建器: 模块要在 <c>OnApplicationInitialization</c> 里直接映射端点时用它
    /// </summary>
    /// <param name="context">应用初始化上下文</param>
    /// <returns>端点路由构建器</returns>
    /// <exception cref="SnowInitializationException">宿主不是 Web 应用, 或还没调用 app.InitializeApplicationAsync()</exception>
    public static IEndpointRouteBuilder GetEndpointRouteBuilder(this ApplicationInitializationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.GetEndpointRouteBuilderOrNull() ?? throw new SnowInitializationException(
            "端点路由构建器尚未就绪: 宿主不是 Web 应用, 或还没调用 app.InitializeApplicationAsync()。");
    }

    /// <summary>
    /// 取端点路由构建器, 取不到返回 null(非 Web 宿主)
    /// </summary>
    /// <param name="context">应用初始化上下文</param>
    /// <returns>端点路由构建器或 null</returns>
    public static IEndpointRouteBuilder? GetEndpointRouteBuilderOrNull(this ApplicationInitializationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.ServiceProvider.GetObjectOrNull<IEndpointRouteBuilder>();
    }

    /// <summary>
    /// 取 Web 应用: 需要 WebApplication 自身(而非 IApplicationBuilder)时用它
    /// </summary>
    /// <param name="context">应用初始化上下文</param>
    /// <returns>Web 应用</returns>
    /// <exception cref="SnowInitializationException">宿主用的是通用主机而非 WebApplication</exception>
    public static WebApplication GetWebApplication(this ApplicationInitializationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.GetWebApplicationOrNull() ?? throw new SnowInitializationException(
            "Web 应用尚未就绪: 宿主用的是通用主机(不是 WebApplication), 或还没调用 app.InitializeApplicationAsync()。");
    }

    /// <summary>
    /// 取 Web 应用, 取不到返回 null(通用主机会返回 null)
    /// </summary>
    /// <param name="context">应用初始化上下文</param>
    /// <returns>Web 应用或 null</returns>
    public static WebApplication? GetWebApplicationOrNull(this ApplicationInitializationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.ServiceProvider.GetObjectOrNull<WebApplication>();
    }

    /// <summary>
    /// 取 Web 宿主环境(环境名、内容根、Web 根)
    /// </summary>
    /// <param name="context">应用初始化上下文</param>
    /// <returns>Web 宿主环境</returns>
    /// <exception cref="InvalidOperationException">容器里没有 IWebHostEnvironment(非 Web 宿主)</exception>
    public static IWebHostEnvironment GetEnvironment(this ApplicationInitializationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
    }

    /// <summary>
    /// 取 Web 宿主环境, 取不到返回 null(非 Web 宿主)
    /// </summary>
    /// <param name="context">应用初始化上下文</param>
    /// <returns>Web 宿主环境或 null</returns>
    public static IWebHostEnvironment? GetEnvironmentOrNull(this ApplicationInitializationContext context)
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

    /// <summary>
    /// 取日志工厂: 初始化阶段还没有 HttpContext, 只能用工厂自己造 logger
    /// </summary>
    /// <param name="context">应用初始化上下文</param>
    /// <returns>日志工厂</returns>
    public static ILoggerFactory GetLoggerFactory(this ApplicationInitializationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.ServiceProvider.GetRequiredService<ILoggerFactory>();
    }
}
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Snow;
using Snow.AspNetCore.Tracing;
using Snow.DependencyInjection;
using Snow.Modularity;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// 宿主的应用初始化入口(对应 ABP 的 <c>AbpApplicationBuilderExtensions</c>)
/// <para><b>宿主显式调用</b>: <c>builder.Build()</c> 之后调一次 <c>await app.InitializeApplicationAsync()</c>,
/// 模块生命周期才开始跑 —— 框架不注册 <c>IStartupFilter</c>, 不在 <c>Build()</c> 里偷偷初始化,
/// 与 ABP 模板的 <c>await app.InitializeApplicationAsync(); await app.RunAsync();</c> 一致。</para>
/// <para>这一次调用按顺序做四件事: ①把应用构建器与宿主放进对象访问器, 让模块的初始化方法拿得到管道;
/// ②把模块关停挂到 <c>IHostApplicationLifetime.ApplicationStopping</c>; ③驱动全部模块的初始化
/// (预初始化 → 第一段 → 收尾段), 启动模块就在第一段里手写整条管道; ④返回后管道已定型, 可以 <c>Run()</c>。</para>
/// </summary>
public static class SnowApplicationBuilderExtensions
{
    private const string ShutdownRegisteredKey = "Snow.ApplicationShutdownRegistered";

    /// <summary>
    /// 初始化应用: 填对象访问器、挂关停钩子、驱动全部模块的初始化阶段
    /// <para>必须在 <c>builder.Build()</c> 之后、<c>app.Run()</c> 之前调用一次。重复调用幂等。</para>
    /// </summary>
    /// <param name="app">应用构建器</param>
    /// <returns>表示初始化过程的任务</returns>
    public static async Task InitializeApplicationAsync(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        SnowApplication application = app.ApplicationServices.GetRequiredService<SnowApplication>();

        FillObjectAccessors(app);
        RegisterShutdown(app, application);

        await application.InitializeAsync(app.ApplicationServices);
    }

    /// <summary>
    /// 挂上链路标识(对应 ABP 的 <c>UseCorrelationId()</c>, 同在 <c>AbpApplicationBuilderExtensions</c> 这个类里)
    /// <para>必须放在所有可能短路的地方之外 —— 认证失败、授权拒绝、路由没匹配时, 响应也要能带回 <c>X-Correlation-Id</c>。</para>
    /// </summary>
    /// <param name="app">应用构建器</param>
    /// <returns>应用构建器本身</returns>
    public static IApplicationBuilder UseSnowCorrelationId(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.UseMiddleware<SnowCorrelationIdMiddleware>();
    }

    /// <summary>
    /// 把模块的关停阶段挂到宿主生命周期上: 主机停机时同步跑一遍
    /// <para>只挂一次: 宿主重复调用初始化不会把关停注册两遍。</para>
    /// </summary>
    /// <param name="app">应用构建器</param>
    /// <param name="application">模块化应用</param>
    private static void RegisterShutdown(IApplicationBuilder app, SnowApplication application)
    {
        if (app.Properties.ContainsKey(ShutdownRegisteredKey))
        {
            return;
        }

        if (app.ApplicationServices.GetService<IHostApplicationLifetime>() is not { } lifetime)
        {
            return;
        }

        app.Properties[ShutdownRegisteredKey] = true;

        lifetime.ApplicationStopping.Register(() => application.Shutdown());
    }

    /// <summary>
    /// 把只有宿主才拿得到的 Web 对象放进对象访问器
    /// </summary>
    /// <param name="app">应用构建器</param>
    private static void FillObjectAccessors(IApplicationBuilder app)
    {
        Set(app.ApplicationServices, app);

        if (app is WebApplication webApplication)
        {
            Set(app.ApplicationServices, webApplication);
        }

        if (app is IEndpointRouteBuilder endpointRouteBuilder)
        {
            Set(app.ApplicationServices, endpointRouteBuilder);
        }

        // WebApplication 本身就是宿主, 优先用它; 通用主机(Startup 风格)下才回落到容器里的 IHost
        if (app is IHost host)
        {
            Set(app.ApplicationServices, host);
        }
        else if (app.ApplicationServices.GetService<IHost>() is { } resolvedHost)
        {
            Set(app.ApplicationServices, resolvedHost);
        }
    }

    /// <summary>
    /// 写一个对象访问器
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <param name="services">根服务提供者</param>
    /// <param name="value">要放进去的对象</param>
    private static void Set<T>(IServiceProvider services, T value) where T : class
    {
        IObjectAccessor<T>? accessor = services.GetService<IObjectAccessor<T>>();
        if (accessor is null)
        {
            throw new SnowInitializationException(
                "找不到 " + typeof(IObjectAccessor<T>).FullName + ": 启动模块没有依赖 SnowAspNetCoreModule。");
        }

        accessor.Value = value;
    }
}

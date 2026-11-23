using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Snow.Modularity;

namespace Snow.AspNetCore;

/// <summary>
/// 框架自己的启动过滤器: 只驱动模块生命周期, 不写管道也不映射端点
/// <para>管道由宿主自己写(对齐 ABP): 宿主在启动模块的 <c>OnApplicationInitialization</c> 里
/// 用 <c>context.GetApplicationBuilder()</c> 按顺序手写 <c>app.UseXxx()</c>, 最后调 <c>app.UseSnowEndpoints()</c> 映射端点。
/// 框架不再铺骨架, 也不再替宿主映射端点。</para>
/// <para>本过滤器做四件事: ①把应用构建器放进对象访问器, 让模块的初始化方法拿得到管道;
/// ②把应用关停挂到 <c>IHostApplicationLifetime.ApplicationStopping</c>; ③驱动预初始化与初始化第一段
/// (宿主在第一段里写管道, 此时仍在 <c>next(app)</c> 之前, 管道能定型); ④驱动收尾段。</para>
/// <para>全部动作必须在 <c>next(app)</c> 之前做完: 构建器一旦交给下一个过滤器,
/// 之后任何 <c>app.Use(...)</c> 都会落到端点之后, 等于不执行。</para>
/// </summary>
internal sealed class SnowAspNetCoreStartupFilter : IStartupFilter
{
    private readonly IServiceProvider _services;

    /// <summary>
    /// 构造启动过滤器
    /// </summary>
    /// <param name="services">根服务提供者</param>
    public SnowAspNetCoreStartupFilter(IServiceProvider services)
    {
        _services = services;
    }

    /// <inheritdoc />
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        ArgumentNullException.ThrowIfNull(next);

        return app =>
        {
            FillObjectAccessors(app);

            SnowApplication application = _services.GetRequiredService<SnowApplication>();

            // 关停钩子: 宿主停机的第一步就通知模块, 此时容器还没释放
            RegisterShutdown(application);

            // 预初始化: 容器已构建、管道还没定型
            application.PreInitializeModulesAsync(app.ApplicationServices).GetAwaiter().GetResult();

            // 第一段: 宿主在这里用 context.GetApplicationBuilder() 手写整条管道, 并最后调 UseSnowEndpoints()
            application.InitializeModulesAsync(app.ApplicationServices).GetAwaiter().GetResult();

            // 收尾段: 管道与端点都已定型, 留作非管道类收尾
            application.PostInitializeModulesAsync().GetAwaiter().GetResult();

            next(app);
        };
    }

    /// <summary>
    /// 把模块的关停阶段挂到宿主生命周期上: 主机停机时同步跑一遍
    /// </summary>
    /// <param name="application">模块化应用</param>
    private void RegisterShutdown(SnowApplication application)
    {
        IHostApplicationLifetime? lifetime = _services.GetService<IHostApplicationLifetime>();
        if (lifetime is null)
        {
            return;
        }

        lifetime.ApplicationStopping.Register(() => application.Shutdown());
    }

    /// <summary>
    /// 把只有宿主才拿得到的 Web 对象放进对象访问器
    /// </summary>
    /// <param name="app">应用构建器</param>
    private void FillObjectAccessors(IApplicationBuilder app)
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

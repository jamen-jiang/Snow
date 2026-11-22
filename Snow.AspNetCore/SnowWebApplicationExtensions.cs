using Microsoft.Extensions.Hosting;
using Snow.Modularity;

namespace Snow.AspNetCore;

/// <summary>
/// Web 宿主接线扩展: 先把只有宿主才有的对象填进对象访问器, 再驱动模块初始化
/// <para>顺序不能反: 模块的 OnApplicationInitialization 阶段就要用这些对象挂中间件与端点。</para>
/// </summary>
public static class SnowWebApplicationExtensions
{
    /// <summary>
    /// 填充 Web 对象访问器: 应用构建器、WebApplication、宿主、端点路由
    /// </summary>
    /// <param name="app">应用构建器</param>
    /// <returns>应用构建器本身, 便于链式调用</returns>
    /// <exception cref="SnowInitializationException">启动模块没有依赖 <see cref="SnowAspNetCoreModule"/></exception>
    public static IApplicationBuilder UseSnowAspNetCore(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        IServiceProvider services = app.ApplicationServices;

        Set(services, app);

        if (app is WebApplication webApplication)
        {
            Set(services, webApplication);
        }

        if (app is IEndpointRouteBuilder endpointRouteBuilder)
        {
            Set(services, endpointRouteBuilder);
        }

        // WebApplication 本身就是宿主, 优先用它; 通用主机(Startup 风格)下才回落到容器里的 IHost
        if (app is IHost host)
        {
            Set(services, host);
        }
        else if (services.GetService<IHost>() is { } resolvedHost)
        {
            Set(services, resolvedHost);
        }

        return app;
    }

    /// <summary>
    /// 填充访问器并驱动全部模块的应用初始化阶段(替代直接调用 <c>app.Services.InitializeAsync()</c>)
    /// </summary>
    /// <param name="app">Web 应用</param>
    /// <returns>Web 应用本身, 便于链式调用</returns>
    public static async Task<WebApplication> InitializeSnowAsync(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseSnowAspNetCore();
        await app.Services.InitializeAsync();

        return app;
    }

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

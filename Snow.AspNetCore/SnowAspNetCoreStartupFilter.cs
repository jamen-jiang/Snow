using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Snow.Modularity;

namespace Snow.AspNetCore;

/// <summary>
/// 框架自己的启动过滤器: 宿主一行初始化代码都不用写
/// <para>ASP.NET Core 在 <c>builder.Build()</c> 之后、管道组装之前执行全部 <see cref="IStartupFilter"/>。
/// 这里做三件事: ①把应用构建器放进对象访问器, 让模块的 <c>OnApplicationInitialization</c> 拿得到管道;
/// ②驱动 <see cref="SnowApplication.InitializeAsync"/>, 各模块的应用初始化阶段因此自动跑起来;
/// ③初始化跑完之后展开 HTTP 管道并映射端点。</para>
/// <para>三件事都必须在 <c>next(app)</c> 之前做完: 构建器一旦交给下一个过滤器,
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

            // 同步等待: 管道必须在管道组装开始之前定型, 这里没有异步余地
            _services.GetRequiredService<SnowApplication>().InitializeAsync(app.ApplicationServices).GetAwaiter().GetResult();

            ExpandPipeline(app);

            next(app);
        };
    }

    /// <summary>
    /// 展开 HTTP 管道: 骨架 + 各包登记的过滤器 + 端点
    /// </summary>
    /// <param name="app">应用构建器</param>
    private void ExpandPipeline(IApplicationBuilder app)
    {
        SnowPipelineOptions options = _services.GetRequiredService<IOptions<SnowPipelineOptions>>().Value;
        ISnowApplicationBuilder builder = app.UseSnow();

        // 默认骨架: 链路标识 → 前置路由钩子 → 路由 → 后置路由钩子 → 认证 → 授权 → 后置钩子。
        // 宿主把 UseDefaultPipeline 关掉时框架一层中间件都不挂, 只剩最后的端点映射
        ISnowEndpointBuilder endpointBuilder = options.UseDefaultPipeline
            ? builder.WithMiddleware(_ => { })
            : builder.WithCustomMiddleware(_ => { });

        endpointBuilder.WithEndpoints(context => SnowAspNetCoreModule.ApplyEndpoints(_services, context.EndpointRouteBuilder));
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

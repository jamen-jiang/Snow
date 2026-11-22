using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Snow.DependencyInjection;

namespace Snow.AspNetCore;

/// <summary>
/// 中间件管道的服务登记入口(供各包在模块里自注册)
/// <para>宿主不要用这里: 宿主手里就有 <c>IApplicationBuilder</c>, 直接 <c>app.Use(...)</c> /
/// <c>app.UseMiddleware&lt;T&gt;()</c>, 或者用 <c>UseSnowStage(阶段, p =&gt; ...)</c> 把自加的中间件与包内的写在一起。</para>
/// </summary>
public static class SnowMiddlewareServiceCollectionExtensions
{
    /// <summary>
    /// 登记一个管道步骤(实现 <see cref="ISnowMiddlewareStep"/>, 由它自己装中间件)
    /// </summary>
    /// <typeparam name="TStep">步骤类型</typeparam>
    /// <param name="services">服务集合</param>
    /// <param name="stage">阶段</param>
    /// <param name="order">阶段内顺序</param>
    /// <returns>服务集合本身</returns>
    public static IServiceCollection AddSnowMiddlewareStep<TStep>(
        this IServiceCollection services, SnowPipelineStage stage, int order)
        where TStep : class, ISnowMiddlewareStep
    {
        ArgumentNullException.ThrowIfNull(services);

        GetOrAddSetup(services).Add(stage, order, typeof(TStep), SnowMiddlewareKind.Step);
        return services;
    }

    /// <summary>
    /// 登记一个中间件类(框架用 <c>UseMiddleware</c> 直接挂, 构造注入照常可用)
    /// </summary>
    /// <typeparam name="TMiddleware">中间件类型</typeparam>
    /// <param name="services">服务集合</param>
    /// <param name="stage">阶段</param>
    /// <param name="order">阶段内顺序</param>
    /// <returns>服务集合本身</returns>
    public static IServiceCollection AddSnowMiddleware<TMiddleware>(
        this IServiceCollection services, SnowPipelineStage stage, int order)
        where TMiddleware : class
    {
        ArgumentNullException.ThrowIfNull(services);

        // IMiddleware 实现由容器按请求构造, 必须自身可解析; 普通中间件多这一条注册也无害
        services.TryAddTransient<TMiddleware>();

        GetOrAddSetup(services).Add(stage, order, typeof(TMiddleware), SnowMiddlewareKind.Middleware);
        return services;
    }

    private static SnowMiddlewareOptionsSetup GetOrAddSetup(IServiceCollection services)
    {
        SnowMiddlewareOptionsSetup? setup = services.GetSingletonInstanceOrNull<SnowMiddlewareOptionsSetup>();
        if (setup is not null)
        {
            return setup;
        }

        setup = new SnowMiddlewareOptionsSetup();
        services.AddSingleton(setup);
        return setup;
    }
}
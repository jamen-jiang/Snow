using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Snow.DependencyInjection;

namespace Snow.AspNetCore;

/// <summary>
/// 管道登记入口: 模块在 <c>ConfigureServices</c> 里一行声明中间件与端点, 宿主不用写管道代码
/// <para>对照 ABP: 不用 <c>Configure&lt;AbpEndpointRouterOptions&gt;</c> 那层包装, 也不需要任何 order 数字,
/// 位置由 <see cref="SnowPipelineStage"/> 表达。</para>
/// </summary>
public static class SnowPipelineServiceCollectionExtensions
{
    /// <summary>
    /// 登记一个中间件到指定阶段
    /// </summary>
    /// <typeparam name="TMiddleware">中间件类型</typeparam>
    /// <param name="services">服务集合</param>
    /// <param name="stage">阶段, 默认路由之前</param>
    /// <returns>服务集合, 便于链式调用</returns>
    public static IServiceCollection AddSnowMiddleware<TMiddleware>(
        this IServiceCollection services,
        SnowPipelineStage stage = SnowPipelineStage.BeforeRouting)
        where TMiddleware : class
    {
        ArgumentNullException.ThrowIfNull(services);

        services.GetSnowPipelineRegistry().Use(stage, typeof(TMiddleware), app => app.UseSnowMiddleware<TMiddleware>());
        return services;
    }

    /// <summary>
    /// 登记一个自定义挂载动作(微软内部中间件只能用官方扩展时走这条)
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="stage">阶段</param>
    /// <param name="configure">挂载动作</param>
    /// <returns>服务集合, 便于链式调用</returns>
    public static IServiceCollection AddSnowMiddleware(
        this IServiceCollection services,
        SnowPipelineStage stage,
        Action<IApplicationBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.GetSnowPipelineRegistry().Use(stage, null, configure);
        return services;
    }

    /// <summary>
    /// 登记一个端点映射动作(Controller / Hub / 健康检查 / 接口文档)
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="map">映射动作</param>
    /// <returns>服务集合, 便于链式调用</returns>
    public static IServiceCollection AddSnowEndpoint(
        this IServiceCollection services,
        Action<IEndpointRouteBuilder> map)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.GetSnowPipelineRegistry().MapEndpoints(map);
        return services;
    }

    /// <summary>
    /// 取管道登记表, 不存在就建一个并登记成单例
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>管道登记表</returns>
    public static SnowPipelineRegistry GetSnowPipelineRegistry(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        SnowPipelineRegistry? registry = services.GetSingletonInstanceOrNull<SnowPipelineRegistry>();
        if (registry is not null)
        {
            return registry;
        }

        registry = new SnowPipelineRegistry();
        services.AddSingleton(registry);
        return registry;
    }
}

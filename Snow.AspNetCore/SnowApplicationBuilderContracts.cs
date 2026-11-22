using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Snow.AspNetCore;

/// <summary>
/// 应用构建器上下文里能拿到的服务
/// </summary>
public interface ISnowApplicationBuilderServices
{
    /// <summary>应用构建器</summary>
    IApplicationBuilder AppBuilder { get; }

    /// <summary>根服务提供者</summary>
    IServiceProvider ApplicationServices { get; }
}

/// <summary>
/// 宿主定制管道时拿到的上下文
/// <para>与 Umbraco 的 <c>IUmbracoApplicationBuilderContext</c> 同形: 既能直接往 <see cref="ISnowApplicationBuilderServices.AppBuilder"/> 上挂,
/// 也能单独重跑某一个位置上的过滤器。</para>
/// </summary>
public interface ISnowApplicationBuilderContext : ISnowApplicationBuilderServices
{
    /// <summary>挂上框架自己的核心中间件(链路标识)</summary>
    void UseSnowCoreMiddleware();

    /// <summary>跑一遍所有过滤器的前置钩子</summary>
    void RunPrePipeline();

    /// <summary>跑一遍所有过滤器的"路由之前"钩子</summary>
    void RunPreRouting();

    /// <summary>跑一遍所有过滤器的"路由之后"钩子</summary>
    void RunPostRouting();

    /// <summary>跑一遍所有过滤器的"骨架之后"钩子</summary>
    void RunPostPipeline();

    /// <summary>挂上框架的默认骨架: 核心中间件 → 路由 → 认证 → 授权</summary>
    void RegisterDefaultRequiredMiddleware();
}

/// <summary>
/// 管道组装入口
/// </summary>
public interface ISnowApplicationBuilder
{
    /// <summary>
    /// 用完全自定义的管道替掉框架默认骨架(过滤器不再执行)
    /// <para>只有确实要自己掌控每一层时才用; 常规做法是让框架展开骨架, 包各自登记过滤器。</para>
    /// </summary>
    /// <param name="configure">往 <see cref="ISnowApplicationBuilderServices.AppBuilder"/> 上挂中间件</param>
    /// <returns>端点阶段入口</returns>
    ISnowEndpointBuilder WithCustomMiddleware(Action<ISnowApplicationBuilderContext> configure);

    /// <summary>
    /// 用框架默认骨架, 再追加宿主自己的中间件
    /// <para>追加的中间件排在骨架最后(授权之后、端点之前)。</para>
    /// </summary>
    /// <param name="configure">往 <see cref="ISnowApplicationBuilderServices.AppBuilder"/> 上挂中间件</param>
    /// <returns>端点阶段入口</returns>
    ISnowEndpointBuilder WithMiddleware(Action<ISnowApplicationBuilderContext> configure);
}

/// <summary>
/// 端点阶段入口
/// </summary>
public interface ISnowEndpointBuilder
{
    /// <summary>
    /// 在 <c>UseEndpoints</c> 里映射端点
    /// </summary>
    /// <param name="configure">往 <see cref="ISnowEndpointBuilderContext.EndpointRouteBuilder"/> 上映射</param>
    void WithEndpoints(Action<ISnowEndpointBuilderContext> configure);
}

/// <summary>
/// 端点阶段上下文
/// </summary>
public interface ISnowEndpointBuilderContext : ISnowApplicationBuilderServices
{
    /// <summary>端点路由构建器</summary>
    IEndpointRouteBuilder EndpointRouteBuilder { get; }
}

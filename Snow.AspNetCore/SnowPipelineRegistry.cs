using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Snow.Modularity;

namespace Snow.AspNetCore;

/// <summary>
/// 管道登记表: 模块用 <c>context.Services.AddSnowMiddleware&lt;T&gt;(stage)</c> 登记中间件,
/// 用 <c>context.Services.AddSnowEndpoint(...)</c> 登记端点映射动作
/// <para>由 <see cref="SnowAspNetCoreModule"/> 在应用初始化阶段按阶段顺序展开, 宿主一行都不用写。
/// 同一个中间件类型只能登记一次, 重复登记在启动期直接报错, 不会静默生效。</para>
/// </summary>
public sealed class SnowPipelineRegistry
{
    private readonly List<SnowPipelineEntry> _middleware = [];
    private readonly List<Action<IEndpointRouteBuilder>> _endpoints = [];
    private readonly Dictionary<Type, SnowPipelineStage> _registered = [];

    /// <summary>
    /// 已登记的中间件, 按登记顺序排列
    /// </summary>
    public IReadOnlyList<SnowPipelineEntry> Middleware => _middleware;

    /// <summary>
    /// 已登记的端点映射动作, 按登记顺序排列
    /// </summary>
    public IReadOnlyList<Action<IEndpointRouteBuilder>> Endpoints => _endpoints;

    /// <summary>
    /// 登记一个中间件挂载动作
    /// </summary>
    /// <param name="stage">阶段</param>
    /// <param name="middlewareType">中间件类型; 为 null 表示这段是自定义挂载, 不参与去重</param>
    /// <param name="configure">挂载动作</param>
    /// <exception cref="SnowInitializationException">同一个中间件类型被登记了两次</exception>
    public void Use(SnowPipelineStage stage, Type? middlewareType, Action<IApplicationBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        if (middlewareType is not null && !_registered.TryAdd(middlewareType, stage))
        {
            throw new SnowInitializationException(
                "中间件 " + middlewareType.FullName + " 已经登记在阶段 " + _registered[middlewareType] +
                ", 不能重复登记到阶段 " + stage + "。");
        }

        _middleware.Add(new SnowPipelineEntry(stage, middlewareType, configure));
    }

    /// <summary>
    /// 登记一个端点映射动作
    /// </summary>
    /// <param name="map">映射动作</param>
    public void MapEndpoints(Action<IEndpointRouteBuilder> map)
    {
        ArgumentNullException.ThrowIfNull(map);

        _endpoints.Add(map);
    }

    /// <summary>
    /// 取某个阶段上的中间件, 保持登记顺序
    /// </summary>
    /// <param name="stage">阶段</param>
    /// <returns>该阶段的中间件登记</returns>
    internal IEnumerable<SnowPipelineEntry> MiddlewareIn(SnowPipelineStage stage)
    {
        return _middleware.Where(entry => entry.Stage == stage);
    }
}

/// <summary>
/// 一条中间件登记
/// </summary>
/// <param name="Stage">阶段</param>
/// <param name="MiddlewareType">中间件类型; 自定义挂载时为 null</param>
/// <param name="Configure">挂载动作</param>
public sealed record SnowPipelineEntry(SnowPipelineStage Stage, Type? MiddlewareType, Action<IApplicationBuilder> Configure);

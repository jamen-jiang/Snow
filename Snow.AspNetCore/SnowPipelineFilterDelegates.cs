using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Snow.AspNetCore;

/// <summary>
/// 用委托实现的管道过滤器: 只给用得到的钩子赋值即可
/// <para>包在 <c>ConfigureServices</c> 里登记:
/// <c>Configure&lt;SnowPipelineOptions&gt;(options => options.AddFilter(new SnowPipelineFilter("包名", preRouting: app => app.UseSnowXxx())))</c>。</para>
/// </summary>
public class SnowPipelineFilter : ISnowPipelineFilter
{
    /// <summary>
    /// 建一个什么都不做的过滤器
    /// </summary>
    /// <param name="name">过滤器名称</param>
    public SnowPipelineFilter(string name)
        : this(name, null, null, null, null, null, null, null)
    {
    }

    /// <summary>
    /// 建一个管道过滤器
    /// </summary>
    /// <param name="name">过滤器名称</param>
    /// <param name="prePipeline">默认中间件之前</param>
    /// <param name="preRouting">路由之前</param>
    /// <param name="postRouting">路由之后、认证之前</param>
    /// <param name="postPipeline">认证与授权之后、端点之前</param>
    /// <param name="endpoints">端点声明之前</param>
    /// <param name="preMapEndpoints">端点声明之前(在 UseEndpoints 里面)</param>
    /// <param name="postMapEndpoints">端点声明之后(在 UseEndpoints 里面)</param>
    public SnowPipelineFilter(
        string name,
        Action<IApplicationBuilder>? prePipeline = null,
        Action<IApplicationBuilder>? preRouting = null,
        Action<IApplicationBuilder>? postRouting = null,
        Action<IApplicationBuilder>? postPipeline = null,
        Action<IApplicationBuilder>? endpoints = null,
        Action<IEndpointRouteBuilder>? preMapEndpoints = null,
        Action<IEndpointRouteBuilder>? postMapEndpoints = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name;
        PrePipeline = prePipeline;
        PreRouting = preRouting;
        PostRouting = postRouting;
        PostPipeline = postPipeline;
        Endpoints = endpoints;
        PreMapEndpoints = preMapEndpoints;
        PostMapEndpoints = postMapEndpoints;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <summary>默认中间件之前</summary>
    public Action<IApplicationBuilder>? PrePipeline { get; set; }

    /// <summary>路由之前</summary>
    public Action<IApplicationBuilder>? PreRouting { get; set; }

    /// <summary>路由之后、认证之前</summary>
    public Action<IApplicationBuilder>? PostRouting { get; set; }

    /// <summary>认证与授权之后、端点之前</summary>
    public Action<IApplicationBuilder>? PostPipeline { get; set; }

    /// <summary>端点声明之前</summary>
    public Action<IApplicationBuilder>? Endpoints { get; set; }

    /// <summary>端点声明之前(在 UseEndpoints 里面)</summary>
    public Action<IEndpointRouteBuilder>? PreMapEndpoints { get; set; }

    /// <summary>端点声明之后(在 UseEndpoints 里面)</summary>
    public Action<IEndpointRouteBuilder>? PostMapEndpoints { get; set; }

    /// <inheritdoc />
    public void OnPrePipeline(IApplicationBuilder app) => PrePipeline?.Invoke(app);

    /// <inheritdoc />
    public void OnPreRouting(IApplicationBuilder app) => PreRouting?.Invoke(app);

    /// <inheritdoc />
    public void OnPostRouting(IApplicationBuilder app) => PostRouting?.Invoke(app);

    /// <inheritdoc />
    public void OnPostPipeline(IApplicationBuilder app) => PostPipeline?.Invoke(app);

    /// <inheritdoc />
    public void OnEndpoints(IApplicationBuilder app) => Endpoints?.Invoke(app);

    /// <inheritdoc />
    public void OnPreMapEndpoints(IEndpointRouteBuilder endpoints) => PreMapEndpoints?.Invoke(endpoints);

    /// <inheritdoc />
    public void OnPostMapEndpoints(IEndpointRouteBuilder endpoints) => PostMapEndpoints?.Invoke(endpoints);
}

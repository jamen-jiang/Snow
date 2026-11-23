namespace Microsoft.AspNetCore.Routing;

/// <summary>
/// 端点映射动作的入参(对应 ABP 的 <c>EndpointRouteBuilderContext</c>, 同在 <c>Microsoft.AspNetCore.Routing</c> 命名空间)
/// <para>动作在一个独立作用域里执行, 所以映射时能安全解析 Scoped 服务。</para>
/// </summary>
public sealed class SnowEndpointRouteBuilderContext
{
    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="endpoints">端点路由构建器</param>
    /// <param name="serviceProvider">映射期间使用的作用域服务提供者</param>
    public SnowEndpointRouteBuilderContext(IEndpointRouteBuilder endpoints, IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentNullException.ThrowIfNull(serviceProvider);

        Endpoints = endpoints;
        ServiceProvider = serviceProvider;
    }

    /// <summary>
    /// 端点路由构建器
    /// </summary>
    public IEndpointRouteBuilder Endpoints { get; }

    /// <summary>
    /// 映射期间使用的作用域服务提供者
    /// </summary>
    public IServiceProvider ServiceProvider { get; }
}

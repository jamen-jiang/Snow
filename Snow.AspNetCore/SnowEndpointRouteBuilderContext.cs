using Microsoft.AspNetCore.Routing;

namespace Snow.AspNetCore;

/// <summary>
/// 一次端点配置的上下文
/// <para>对应 ABP 的 <c>EndpointRouteBuilderContext</c>: 既给映射端点用的 <see cref="IEndpointRouteBuilder"/>,
/// 也给一个作用域容器, 便于在映射时解析服务。</para>
/// </summary>
public sealed class SnowEndpointRouteBuilderContext
{
    /// <summary>
    /// 创建上下文
    /// </summary>
    /// <param name="endpoints">端点路由构建器</param>
    /// <param name="serviceProvider">作用域容器</param>
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
    /// 作用域容器
    /// </summary>
    public IServiceProvider ServiceProvider { get; }
}

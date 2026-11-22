using Microsoft.AspNetCore.Routing;

namespace Snow.AspNetCore;

/// <summary>
/// 端点路由选项(对应 ABP 的 <c>AbpEndpointRouterOptions</c>)
/// <para>端点与中间件不是一回事: 中间件由宿主按行序写 <c>app.UseXxx()</c>, 端点则挂在管道的最后一段 ——
/// 位置唯一, 不需要任何顺序信息, 所以模块自己交进来就行。</para>
/// <para>框架在全部模块初始化完成之后把这些动作统一执行掉, 宿主不用写, 也没有对应的公开方法可调。</para>
/// </summary>
public sealed class SnowEndpointRouterOptions
{
    /// <summary>
    /// 端点映射动作清单, 按登记先后执行
    /// </summary>
    public List<Action<SnowEndpointRouteBuilderContext>> EndpointConfigureActions { get; } = [];
}

/// <summary>
/// 端点映射动作的入参(对应 ABP 的 <c>EndpointRouteBuilderContext</c>)
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

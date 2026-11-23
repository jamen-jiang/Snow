namespace Microsoft.AspNetCore.Routing;

/// <summary>
/// 端点路由选项(对应 ABP 的 <c>AbpEndpointRouterOptions</c>, 同在 <c>Microsoft.AspNetCore.Routing</c> 命名空间)
/// <para>端点与中间件不是一回事: 中间件由宿主按行序写 <c>app.UseXxx()</c>, 端点则挂在管道的最后一段 ——
/// 位置唯一, 不需要任何顺序信息, 所以模块自己交进来就行。</para>
/// <para>宿主在管道最后一步调 <c>app.UseSnowEndpoints()</c> 把这些动作统一执行掉, 与 ABP 模板的
/// <c>app.UseConfiguredEndpoints()</c> 同形。</para>
/// </summary>
public sealed class SnowEndpointRouterOptions
{
    /// <summary>
    /// 端点映射动作清单, 按登记先后执行
    /// </summary>
    public List<Action<SnowEndpointRouteBuilderContext>> EndpointConfigureActions { get; } = [];
}

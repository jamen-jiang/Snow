using Microsoft.AspNetCore.Routing;

namespace Snow.AspNetCore;

/// <summary>
/// 端点挂载登记表
/// <para>对应 ABP 的 <c>AbpEndpointRouterOptions</c>。模块在 ConfigureServices 阶段把自己的映射动作追加进来,
/// 宿主在 <c>UseSnowEndpoints()</c> 里统一执行 —— 因为映射端点需要的是 <see cref="IEndpointRouteBuilder"/>,
/// 而模块的应用初始化阶段只拿得到 <see cref="Microsoft.AspNetCore.Builder.IApplicationBuilder"/>。</para>
/// </summary>
public sealed class SnowEndpointRouterOptions
{
    /// <summary>
    /// 各模块贡献的端点配置动作, 按登记顺序执行
    /// </summary>
    public List<Action<SnowEndpointRouteBuilderContext>> EndpointConfigureActions { get; } = [];
}

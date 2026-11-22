using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Snow.AspNetCore;

/// <summary>
/// HTTP 管道过滤器: 包把自己的中间件挂进框架的固定骨架, 不用知道别人的存在
/// <para>框架的骨架是写死的(<c>前置 → 链路标识 → 前置路由 → 路由 → 后置路由 → 认证 → 授权 → 后置 → 端点</c>),
/// 包只能选一个位置把中间件放进去, 宿主一行管道代码都不用写。</para>
/// <para>位置是具名语义, 不是数字也不是阶段枚举: 同一个位置里多个过滤器按登记先后执行,
/// 也就是模块依赖的拓扑序 —— 被依赖的模块先登记。</para>
/// <para>实现方式两种: 继承 <see cref="SnowPipelineFilter"/> 在构造里给委托, 或直接实现本接口。</para>
/// </summary>
public interface ISnowPipelineFilter
{
    /// <summary>
    /// 过滤器名称, 启动日志里打出来, 便于确认挂了什么、按什么顺序挂的
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 在框架的默认中间件之前执行(链路标识之前)
    /// <para>整个管道的最外层, 适合必须包住一切的东西。</para>
    /// </summary>
    /// <param name="app">应用构建器</param>
    void OnPrePipeline(IApplicationBuilder app);

    /// <summary>
    /// 在静态资源中间件之后、<c>UseRouting()</c> 之前执行
    /// <para>此时链路标识已挂上, 路由还没匹配端点。</para>
    /// </summary>
    /// <param name="app">应用构建器</param>
    void OnPreRouting(IApplicationBuilder app);

    /// <summary>
    /// 在 <c>UseRouting()</c> 之后、认证与授权之前执行
    /// <para>CORS 放这里: 它要看到端点元数据, 又必须在授权拒绝之前给出响应头。</para>
    /// </summary>
    /// <param name="app">应用构建器</param>
    void OnPostRouting(IApplicationBuilder app);

    /// <summary>
    /// 在框架默认中间件(认证、授权)之后、端点声明之前执行
    /// <para>要读已认证主体的中间件放这里。</para>
    /// </summary>
    /// <param name="app">应用构建器</param>
    void OnPostPipeline(IApplicationBuilder app);

    /// <summary>
    /// 在 <c>UseEndpoints</c> 之前执行
    /// </summary>
    /// <param name="app">应用构建器</param>
    void OnEndpoints(IApplicationBuilder app);

    /// <summary>
    /// 在 <c>UseEndpoints</c> 里面、端点声明之前执行
    /// </summary>
    /// <param name="endpoints">端点路由构建器</param>
    void OnPreMapEndpoints(IEndpointRouteBuilder endpoints);

    /// <summary>
    /// 在 <c>UseEndpoints</c> 里面、端点声明之后执行
    /// </summary>
    /// <param name="endpoints">端点路由构建器</param>
    void OnPostMapEndpoints(IEndpointRouteBuilder endpoints);
}

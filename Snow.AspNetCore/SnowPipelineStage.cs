namespace Snow.AspNetCore;

/// <summary>
/// 中间件管道阶段: 宿主管道里的锚点位置
/// <para>ASP.NET Core 的管道是代码顺序链表, 没有 order 数字, 所以框架只能提供少数固定锚点:
/// 宿主在每个锚点调用一次 <c>UseSnowPipeline(阶段)</c>, 各包把中间件登记到某个阶段, 之后加中间件不再改宿主。</para>
/// <para>枚举值有间隔, 便于以后插入新阶段而不影响已有含义。</para>
/// </summary>
public enum SnowPipelineStage
{
    /// <summary>路由之前: 链路标识、日志上下文增强等最外层中间件</summary>
    BeforeRouting = 0,

    /// <summary>路由之后、认证之前: 需要路由值但不需要当前用户的中间件</summary>
    AfterRouting = 1000,

    /// <summary>认证之后、授权之前: 多租户等需要当前用户声明的中间件</summary>
    AfterAuthentication = 2000,

    /// <summary>授权之后、端点映射之前: 工作单元、审计等需要授权结果的中间件</summary>
    AfterAuthorization = 3000,
}

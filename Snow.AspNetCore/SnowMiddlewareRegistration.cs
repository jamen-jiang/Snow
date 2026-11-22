namespace Snow.AspNetCore;

/// <summary>
/// 中间件登记项的挂载方式
/// </summary>
public enum SnowMiddlewareKind
{
    /// <summary>实现 <see cref="ISnowMiddlewareStep"/> 的登记器, 由它自己决定怎么挂</summary>
    Step = 0,

    /// <summary>中间件类本身, 由框架用 <c>UseMiddleware</c> 直接挂</summary>
    Middleware = 1,
}

/// <summary>
/// 一条中间件登记项
/// </summary>
/// <param name="Stage">挂在哪个阶段</param>
/// <param name="Order">阶段内的顺序, 小的先执行</param>
/// <param name="Sequence">登记先后, 用于同顺序时的稳定排序</param>
/// <param name="ImplementationType">登记器类型或中间件类型</param>
/// <param name="Kind">挂载方式</param>
public sealed record SnowMiddlewareRegistration(
    SnowPipelineStage Stage,
    int Order,
    int Sequence,
    Type ImplementationType,
    SnowMiddlewareKind Kind)
{
    /// <summary>
    /// 去重键: 用于"同一中间件被手动挂载与管道展开各一次"时只生效一次
    /// </summary>
    public string Key => ImplementationType.FullName ?? ImplementationType.Name;
}

namespace Snow.AspNetCore;

/// <summary>
/// 框架内置中间件的顺序常量
/// <para>业务与宿主不要写裸数字: 同一阶段内用 <see cref="HostOffset"/> 往后递增, 避免与框架内部值撞车。</para>
/// <para>同一阶段内出现重复顺序会在启动时抛异常, 不做隐式竞猜。</para>
/// </summary>
public static class SnowMiddlewareOrders
{
    /// <summary>链路标识: 最外层, 让后续所有日志/审计都带上它</summary>
    public const int CorrelationId = -1000;

    /// <summary>日志上下文增强: 紧跟在链路标识之后</summary>
    public const int SerilogEnrichers = -900;

    /// <summary>多租户: 认证之后</summary>
    public const int MultiTenancy = 0;

    /// <summary>工作单元: 多租户之后</summary>
    public const int UnitOfWork = 100;

    /// <summary>宿主自定义中间件的起始顺序值</summary>
    public const int HostOffset = 10_000;
}

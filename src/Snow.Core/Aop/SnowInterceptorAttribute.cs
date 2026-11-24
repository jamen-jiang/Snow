namespace Snow.Aop;

/// <summary>
/// 拦截器: 继承它并重写 <see cref="InvokeAsync"/> 就得到一个拦截器, 直接标在类或方法上即生效。
/// <example>
/// <code>
/// public sealed class TraceAttribute : SnowInterceptorAttribute
/// {
///     public override async Task InvokeAsync(AspectContext context, AspectDelegate next, CancellationToken cancellationToken = default)
///     {
///         // 前置
///         await next(context, cancellationToken);
///         // 后置, 可用 context.ReturnValue
///     }
/// }
///
/// [Trace]
/// public sealed class OrderService : IOrderService { ... }
/// </code>
/// </example>
/// <para>标在类上对全部接口方法生效; 标在实现方法上只对该方法生效, 两者叠加时类上的在外层。</para>
/// <para>特性实例由反射创建并按服务类型共享, 因此实现必须无状态且线程安全; 需要其他服务时从
/// <see cref="AspectContext.ServiceProvider"/> 解析(特性不支持构造注入)。</para>
/// <para>不调 <c>next</c> 即短路, 此时必须给 <see cref="AspectContext.ReturnValue"/> 赋值;
/// 异步方法的短路结果由框架按声明返回类型包装。</para>
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public abstract class SnowInterceptorAttribute : Attribute
{
    /// <summary>
    /// 拦截一次调用
    /// </summary>
    /// <param name="context">调用上下文, 整条链共享</param>
    /// <param name="next">拦截链的下一环</param>
    /// <param name="cancellationToken">取消令牌, 取自被拦截方法的 CancellationToken 参数</param>
    /// <returns>表示拦截过程的任务</returns>
    public abstract Task InvokeAsync(AspectContext context, AspectDelegate next, CancellationToken cancellationToken = default);
}
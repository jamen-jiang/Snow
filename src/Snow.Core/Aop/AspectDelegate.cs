namespace Snow.Aop;

/// <summary>
/// 拦截链的下一环: 与 ASP.NET Core 的 <c>RequestDelegate</c> 同形, 由上一层当参数传下去。
/// <para>拦截器调 <c>await next(context, cancellationToken)</c> 才会走到目标方法; 不调即短路, 此时要给
/// <see cref="AspectContext.ReturnValue"/> 赋值。</para>
/// </summary>
/// <param name="context">调用上下文, 整条链共享</param>
/// <param name="cancellationToken">取消令牌, 取自被拦截方法的 CancellationToken 参数</param>
/// <returns>表示拦截链其余部分的任务</returns>
public delegate Task AspectDelegate(AspectContext context, CancellationToken cancellationToken = default);
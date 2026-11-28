using System.Threading.Tasks;

namespace Snow.DynamicProxy;

/// <summary>
/// <see cref="ISnowInterceptor"/> 的抽象基类: 实现方只需重写 <see cref="InterceptAsync"/>。
/// </summary>
public abstract class SnowInterceptor : ISnowInterceptor
{
    /// <summary>
    /// 拦截一次方法调用
    /// </summary>
    /// <param name="invocation">调用信息; 只有调用了 <see cref="ISnowMethodInvocation.ProceedAsync"/> 才会执行目标方法</param>
    /// <returns>表示拦截过程的任务</returns>
    public abstract Task InterceptAsync(ISnowMethodInvocation invocation);
}

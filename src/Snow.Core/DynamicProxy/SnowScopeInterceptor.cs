using System.Threading.Tasks;

namespace Snow.DynamicProxy;

/// <summary>
/// 作用域式拦截器基类: 把「开作用域 → 执行 → 完成/失败 → 释放」这段固定流程收在基类里, 子类只需给出怎么开作用域。
/// <para>返回 <see cref="System.Collections.Generic.IAsyncEnumerable{T}"/> 时, 每次枚举都会新建一个作用域。</para>
/// </summary>
public abstract class SnowScopeInterceptor : SnowInterceptor
{
    /// <summary>
    /// 拦截一次方法调用: 需要拦截时在整个执行过程的范围内跑, 否则直接放行
    /// </summary>
    /// <param name="invocation">调用信息</param>
    /// <returns>表示拦截过程的任务</returns>
    public override Task InterceptAsync(ISnowMethodInvocation invocation)
    {
        return ShouldIntercept(invocation)
            ? invocation.ProceedInScopeAsync(() => BeginScope(invocation))
            : invocation.ProceedAsync();
    }

    /// <summary>
    /// 判断本次调用是否需要开作用域
    /// </summary>
    /// <param name="invocation">调用信息</param>
    /// <returns>需要拦截时返回 <see langword="true"/></returns>
    protected virtual bool ShouldIntercept(ISnowMethodInvocation invocation)
    {
        return true;
    }

    /// <summary>
    /// 开一个作用域: 必须同步设置好异步本地值(例如环境工作单元), 否则枚举异步流时看不到
    /// </summary>
    /// <param name="invocation">调用信息</param>
    /// <returns>新建的作用域</returns>
    protected abstract ISnowInterceptionScope BeginScope(ISnowMethodInvocation invocation);
}

using System.Threading.Tasks;

namespace Snow.DynamicProxy;

/// <summary>
/// 拦截器契约: 以独立服务形式注册的拦截器实现它, 再由 <c>SnowAsyncDeterminationInterceptor{T}</c> 挂到代理上。
/// <para>与 ABP 的 <c>IAbpInterceptor</c> 同形: 实例由容器提供, 具体挂到哪些服务上由各模块的登记器
/// (监听 <c>OnRegistered</c>, 往 <c>Interceptors</c> 里 TryAdd)决定, 最终都跑在同一套 <see cref="ISnowMethodInvocation"/> 上。</para>
/// </summary>
public interface ISnowInterceptor
{
    /// <summary>
    /// 拦截一次方法调用
    /// </summary>
    /// <param name="invocation">调用信息; 只有调用了 <see cref="ISnowMethodInvocation.ProceedAsync"/> 才会执行目标方法</param>
    /// <returns>表示拦截过程的任务</returns>
    Task InterceptAsync(ISnowMethodInvocation invocation);
}

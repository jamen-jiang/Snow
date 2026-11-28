using System;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using Snow.DynamicProxy;

namespace Snow.Castle.DynamicProxy;

/// <summary>
/// 把 <see cref="ISnowInterceptor"/> 适配到 Castle 的异步拦截基类, 与 ABP 的 <c>CastleAsyncAbpInterceptorAdapter</c> 同形。
/// <para>有返回值的方法走 <see cref="CastleSnowMethodInvocationAdapterWithReturnValue{TResult}"/>, 拦截结束后取适配器上的返回值。</para>
/// </summary>
/// <typeparam name="TInterceptor">实际的拦截器类型</typeparam>
public class CastleAsyncSnowInterceptorAdapter<TInterceptor> : AsyncInterceptorBase
    where TInterceptor : ISnowInterceptor
{
    private readonly TInterceptor _snowInterceptor;

    /// <summary>
    /// 创建适配器
    /// </summary>
    /// <param name="snowInterceptor">实际的拦截器</param>
    public CastleAsyncSnowInterceptorAdapter(TInterceptor snowInterceptor)
    {
        _snowInterceptor = snowInterceptor;
    }

    /// <inheritdoc />
    protected override async Task InterceptAsync(IInvocation invocation, IInvocationProceedInfo proceedInfo, Func<IInvocation, IInvocationProceedInfo, Task> proceed)
    {
        await _snowInterceptor.InterceptAsync(
            new CastleSnowMethodInvocationAdapter(invocation, proceedInfo, proceed)
        );
    }

    /// <inheritdoc />
    protected override async Task<TResult> InterceptAsync<TResult>(IInvocation invocation, IInvocationProceedInfo proceedInfo, Func<IInvocation, IInvocationProceedInfo, Task<TResult>> proceed)
    {
        CastleSnowMethodInvocationAdapterWithReturnValue<TResult> adapter =
            new(invocation, proceedInfo, proceed);

        await _snowInterceptor.InterceptAsync(adapter);

        return (TResult)adapter.ReturnValue!;
    }
}

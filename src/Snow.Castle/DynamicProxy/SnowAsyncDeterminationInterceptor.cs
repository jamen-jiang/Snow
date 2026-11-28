using Castle.DynamicProxy;
using Snow.DynamicProxy;

namespace Snow.Castle.DynamicProxy;

/// <summary>
/// 按返回类型自动选择同步/异步拦截路径的 Castle 拦截器, 与 ABP 的 <c>AbpAsyncDeterminationInterceptor</c> 同形。
/// <para>返回 <see cref="System.Collections.Generic.IAsyncEnumerable{T}"/> 时交给 <see cref="CastleAsyncSnowInterceptor"/> 特殊处理。</para>
/// <para>按泛型定义在 <see cref="SnowCastleCoreModule"/> 里显式登记, 与 ABP 的 <c>AbpCastleCoreModule</c> 同形。</para>
/// </summary>
/// <typeparam name="TInterceptor">实际的拦截器类型</typeparam>
public class SnowAsyncDeterminationInterceptor<TInterceptor> : AsyncDeterminationInterceptor
    where TInterceptor : ISnowInterceptor
{
    private readonly TInterceptor _snowInterceptor;

    /// <summary>
    /// 创建拦截器
    /// </summary>
    /// <param name="snowInterceptor">实际的拦截器</param>
    public SnowAsyncDeterminationInterceptor(TInterceptor snowInterceptor)
        : base(new CastleAsyncSnowInterceptorAdapter<TInterceptor>(snowInterceptor))
    {
        _snowInterceptor = snowInterceptor;
    }

    /// <summary>
    /// 执行拦截
    /// </summary>
    /// <param name="invocation">Castle 的调用信息</param>
    public override void Intercept(IInvocation invocation)
    {
        if (CastleAsyncSnowInterceptor.TryIntercept(invocation, _snowInterceptor))
        {
            return;
        }

        base.Intercept(invocation);
    }
}

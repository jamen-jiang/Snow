using System;
using System.Threading.Tasks;
using Castle.DynamicProxy;

namespace Snow.Castle.DynamicProxy;

/// <summary>
/// 有返回值方法的调用适配器, 与 ABP 的 <c>CastleAbpMethodInvocationAdapterWithReturnValue</c> 同形。
/// </summary>
/// <typeparam name="TResult">目标方法的返回类型</typeparam>
public class CastleSnowMethodInvocationAdapterWithReturnValue<TResult> : CastleSnowMethodInvocationAdapterBase
{
    /// <summary>
    /// 继续执行下一环所需的信息
    /// </summary>
    protected IInvocationProceedInfo ProceedInfo { get; }

    /// <summary>
    /// 继续执行下一环的委托
    /// </summary>
    protected Func<IInvocation, IInvocationProceedInfo, Task<TResult>> Proceed { get; }

    /// <summary>
    /// 创建适配器
    /// </summary>
    /// <param name="invocation">Castle 的调用信息</param>
    /// <param name="proceedInfo">继续执行下一环所需的信息</param>
    /// <param name="proceed">继续执行下一环的委托</param>
    public CastleSnowMethodInvocationAdapterWithReturnValue(IInvocation invocation,
        IInvocationProceedInfo proceedInfo,
        Func<IInvocation, IInvocationProceedInfo, Task<TResult>> proceed)
        : base(invocation)
    {
        ProceedInfo = proceedInfo;
        Proceed = proceed;
    }

    /// <inheritdoc />
    public override async Task ProceedAsync()
    {
        ReturnValue = (await Proceed(Invocation, ProceedInfo))!;
    }
}

using System;
using System.Threading.Tasks;
using Castle.DynamicProxy;

namespace Snow.Castle.DynamicProxy;

/// <summary>
/// 无返回值(或返回 <see cref="Task"/>)方法的调用适配器, 与 ABP 的 <c>CastleAbpMethodInvocationAdapter</c> 同形。
/// </summary>
public class CastleSnowMethodInvocationAdapter : CastleSnowMethodInvocationAdapterBase
{
    /// <summary>
    /// 继续执行下一环所需的信息
    /// </summary>
    protected IInvocationProceedInfo ProceedInfo { get; }

    /// <summary>
    /// 继续执行下一环的委托
    /// </summary>
    protected Func<IInvocation, IInvocationProceedInfo, Task> Proceed { get; }

    /// <summary>
    /// 创建适配器
    /// </summary>
    /// <param name="invocation">Castle 的调用信息</param>
    /// <param name="proceedInfo">继续执行下一环所需的信息</param>
    /// <param name="proceed">继续执行下一环的委托</param>
    public CastleSnowMethodInvocationAdapter(IInvocation invocation, IInvocationProceedInfo proceedInfo,
        Func<IInvocation, IInvocationProceedInfo, Task> proceed)
        : base(invocation)
    {
        ProceedInfo = proceedInfo;
        Proceed = proceed;
    }

    /// <inheritdoc />
    public override async Task ProceedAsync()
    {
        await Proceed(Invocation, ProceedInfo);
    }
}

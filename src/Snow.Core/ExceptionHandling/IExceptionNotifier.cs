using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Snow.ExceptionHandling;

/// <summary>
/// 异常通知器(对应 ABP 的 <c>IExceptionNotifier</c>): 把一次异常分发给所有已登记的 <see cref="IExceptionSubscriber"/>。
/// </summary>
public interface IExceptionNotifier
{
    /// <summary>
    /// 通知所有订阅者
    /// </summary>
    /// <param name="context">异常通知上下文</param>
    Task NotifyAsync([NotNull] ExceptionNotificationContext context);
}

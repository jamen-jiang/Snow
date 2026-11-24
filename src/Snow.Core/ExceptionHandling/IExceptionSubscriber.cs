using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Snow.ExceptionHandling;

/// <summary>
/// 异常订阅者(对应 ABP 的 <c>IExceptionSubscriber</c>): 收到异常通知时做记录、上报等副作用。
/// </summary>
public interface IExceptionSubscriber
{
    /// <summary>
    /// 处理一次异常通知
    /// </summary>
    /// <param name="context">异常通知上下文</param>
    Task HandleAsync([NotNull] ExceptionNotificationContext context);
}

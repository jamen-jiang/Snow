using System;
using System.Threading;
using System.Threading.Tasks;

namespace Snow.DynamicProxy;

/// <summary>
/// 一次拦截的作用域: 用于在方法执行前后成对收尾(例如开启/提交工作单元), 覆盖返回的异步流被提前停止枚举的情形。
/// </summary>
public interface ISnowInterceptionScope : IDisposable
{
    /// <summary>
    /// 正常完成时调用; 调用方提前停止枚举返回的异步流时也会调用
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>表示收尾过程的任务</returns>
    Task CompleteAsync(CancellationToken cancellationToken);

    /// <summary>
    /// 目标方法抛异常时调用
    /// </summary>
    /// <param name="exception">目标方法抛出的异常</param>
    /// <returns>表示收尾过程的任务</returns>
    Task FailAsync(Exception exception);
}

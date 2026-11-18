namespace Rongia.Messaging;

using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// 管道中的下一环:由管道实现方提供,行为内部按需 await 它继续向内传递
/// </summary>
/// <typeparam name="TResponse">返回值类型</typeparam>
/// <param name="t">取消令牌</param>
/// <returns>最终结果</returns>
public delegate Task<TResponse> RequestHandlerDelegate<TResponse>(CancellationToken t = default);

/// <summary>
/// 管道行为:包裹真正的处理器,在调用前后插入横切逻辑(日志、校验、事务、锁等)
/// </summary>
/// <typeparam name="TRequest">请求类型</typeparam>
/// <typeparam name="TResponse">返回值类型</typeparam>
/// <remarks>
/// 多个行为的执行顺序由 IOC 注册顺序决定:先注册的在外层(先进入、后退出)。
/// </remarks>
public interface IPipelineBehavior<in TRequest, TResponse> where TRequest : notnull
{
    /// <summary>
    /// 执行行为:完成自身逻辑后 await <paramref name="next"/> 继续向内调用
    /// </summary>
    /// <param name="request">进来的请求</param>
    /// <param name="next">通向下一环(最终是处理器)的委托</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>最终结果</returns>
    Task<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken);
}

namespace Rongia.Messaging;

/// <summary>
/// 查询的处理器(与 <see cref="ICommandHandler{TCommand,TResponse}"/> 完全独立,互不继承)
/// </summary>
/// <typeparam name="TQuery">查询类型</typeparam>
/// <typeparam name="TResponse">返回值类型</typeparam>
public interface IQueryHandler<in TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    /// <summary>
    /// 处理查询
    /// </summary>
    /// <param name="query">查询实例</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>查询结果</returns>
    Task<TResponse> HandleAsync(TQuery query, CancellationToken cancellationToken);
}

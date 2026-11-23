namespace Snow.Messaging;

/// <summary>
/// 查询总线:把查询沿管道送给唯一的处理器(读侧;写侧见 <see cref="ICommandBus"/>)
/// </summary>
public interface IQueryBus
{
    /// <summary>
    /// 发送查询
    /// </summary>
    /// <typeparam name="TResponse">返回值类型</typeparam>
    /// <param name="query">查询实例</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>查询结果</returns>
    Task<TResponse> QueryAsync<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken = default);
}

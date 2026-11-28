namespace Snow.Uow;

/// <summary>
/// 事务 API(对应 ABP 的 <c>ITransactionApi</c>): 工作单元提交时统一提交, 释放时统一销毁
/// </summary>
public interface ITransactionApi : IDisposable
{
    /// <summary>
    /// 提交事务
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步任务</returns>
    Task CommitAsync(CancellationToken cancellationToken = default);
}

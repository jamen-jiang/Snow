namespace Snow.Uow;

/// <summary>
/// 支持回滚的 API(对应 ABP 的 <c>ISupportsRollback</c>)
/// </summary>
public interface ISupportsRollback
{
    /// <summary>
    /// 回滚
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步任务</returns>
    Task RollbackAsync(CancellationToken cancellationToken = default);
}

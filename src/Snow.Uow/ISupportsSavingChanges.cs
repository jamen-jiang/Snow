namespace Snow.Uow;

/// <summary>
/// 支持保存数据的 API(对应 ABP 的 <c>ISupportsSavingChanges</c>): 工作单元提交前会挨个调它
/// </summary>
public interface ISupportsSavingChanges
{
    /// <summary>
    /// 保存数据(不提交事务)
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步任务</returns>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

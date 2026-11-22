namespace Snow.Auditing;

/// <summary>
/// 审计日志存储: 换实现即可落库到任意介质
/// <para>宿主在 <c>ConfigureServices</c> 里 <c>TryAddSingleton&lt;IAuditingStore, MyStore&gt;()</c> 即可顶掉默认的空实现(约定注册会跳过已显式登记的服务类型)。</para>
/// </summary>
public interface IAuditingStore
{
    /// <summary>
    /// 保存一条审计日志
    /// </summary>
    /// <param name="auditLogInfo">审计日志</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>表示保存过程的任务</returns>
    Task SaveAsync(AuditLogInfo auditLogInfo, CancellationToken cancellationToken = default);
}

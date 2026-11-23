namespace Snow.Auditing;

/// <summary>
/// 审计管理器: 管理当前审计作用域并把日志交给 <see cref="IAuditingStore"/>
/// </summary>
public interface IAuditingManager
{
    /// <summary>
    /// 当前审计作用域, 不在审计上下文中时为 null
    /// </summary>
    IAuditLogScope? Current { get; }

    /// <summary>
    /// 开启一个审计作用域
    /// </summary>
    /// <param name="logInfo">本作用域要收集的日志</param>
    /// <returns>作用域, 释放后恢复上一层</returns>
    IAuditLogScope BeginScope(AuditLogInfo logInfo);

    /// <summary>
    /// 保存当前作用域的日志
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否写入成功</returns>
    Task<bool> SaveAsync(CancellationToken cancellationToken = default);
}

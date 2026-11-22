namespace Snow.Auditing;

/// <summary>
/// 审计作用域: 生命周期内收集一条审计日志
/// <para>释放时恢复上一层的当前作用域, 因此嵌套调用里的审计互不覆盖。</para>
/// </summary>
public interface IAuditLogScope : IDisposable
{
    /// <summary>
    /// 本作用域收集的审计日志
    /// </summary>
    AuditLogInfo LogInfo { get; }
}

/// <summary>
/// 审计作用域默认实现
/// </summary>
internal sealed class AuditLogScope : IAuditLogScope
{
    private readonly Action _onDispose;

    public AuditLogScope(AuditLogInfo logInfo, Action onDispose)
    {
        LogInfo = logInfo;
        _onDispose = onDispose;
    }

    public AuditLogInfo LogInfo { get; }

    public void Dispose()
    {
        _onDispose();
    }
}

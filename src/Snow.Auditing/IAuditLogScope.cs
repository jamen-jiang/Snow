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

    /// <summary>
    /// 用给定的审计日志和释放回调创建作用域
    /// </summary>
    /// <param name="logInfo">本作用域收集的审计日志</param>
    /// <param name="onDispose">作用域释放时要执行的回调(通常是恢复上一层作用域)</param>
    public AuditLogScope(AuditLogInfo logInfo, Action onDispose)
    {
        LogInfo = logInfo;
        _onDispose = onDispose;
    }

    public AuditLogInfo LogInfo { get; }

    /// <summary>
    /// 结束作用域, 执行释放回调
    /// </summary>
    public void Dispose()
    {
        _onDispose();
    }
}

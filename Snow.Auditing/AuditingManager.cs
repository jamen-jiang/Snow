using Microsoft.Extensions.Logging;
using Snow.DependencyInjection;

namespace Snow.Auditing;

/// <summary>
/// 审计管理器默认实现
/// <para>作用域用实例字段串成栈: 同一个请求共用这个作用域实例, 嵌套调用各自压栈、出栈时还原。</para>
/// <para>保存失败只记错误日志, 不向业务抛: 审计不能把正常调用拖垮。</para>
/// </summary>
[ScopedService]
public sealed class AuditingManager : IAuditingManager
{
    private readonly IAuditingStore _store;
    private readonly ILogger<AuditingManager> _logger;
    private IAuditLogScope? _current;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="store">审计存储</param>
    /// <param name="logger">日志</param>
    public AuditingManager(IAuditingStore store, ILogger<AuditingManager> logger)
    {
        _store = store;
        _logger = logger;
    }

    /// <inheritdoc />
    public IAuditLogScope? Current => _current;

    /// <inheritdoc />
    public IAuditLogScope BeginScope(AuditLogInfo logInfo)
    {
        ArgumentNullException.ThrowIfNull(logInfo);

        IAuditLogScope? parent = _current;
        AuditLogScope scope = new(logInfo, () => _current = parent);
        _current = scope;

        return scope;
    }

    /// <inheritdoc />
    public async Task<bool> SaveAsync(CancellationToken cancellationToken = default)
    {
        AuditLogInfo? logInfo = _current?.LogInfo;
        if (logInfo is null)
        {
            return false;
        }

        try
        {
            await _store.SaveAsync(logInfo, cancellationToken).ConfigureAwait(false);

            return true;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "保存审计日志失败: {LogId}", logInfo.Id);

            return false;
        }
    }
}

using Microsoft.Extensions.Logging;
using Snow.DependencyInjection;

namespace Snow.Auditing;

/// <summary>
/// 默认审计存储: 只写调试日志, 不落任何介质
/// <para>有它才能保证「没配存储也能跑」; 要真落库请替换 <see cref="IAuditingStore"/>。</para>
/// </summary>
[SingletonService]
public sealed class NullAuditingStore : IAuditingStore
{
    private readonly ILogger<NullAuditingStore> _logger;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="logger">日志</param>
    public NullAuditingStore(ILogger<NullAuditingStore> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task SaveAsync(AuditLogInfo auditLogInfo, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(auditLogInfo);

        _logger.LogDebug(
            "审计日志未落库(默认 NullAuditingStore): {ServiceName}.{MethodName}, 用户 {UserName}, 耗时 {Duration}ms, 异常 {HasException}",
            auditLogInfo.Actions.FirstOrDefault()?.ServiceName,
            auditLogInfo.Actions.FirstOrDefault()?.MethodName,
            auditLogInfo.UserName,
            auditLogInfo.ExecutionDuration,
            auditLogInfo.HasException);

        return Task.CompletedTask;
    }
}

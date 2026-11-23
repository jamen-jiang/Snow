namespace Snow.Auditing;

/// <summary>
/// 审计日志里记录的异常快照
/// <para>只留类型、消息与堆栈, 不持有 <see cref="Exception"/> 实例: 日志要能跨进程序列化落库。</para>
/// </summary>
public sealed class AuditLogException
{
    /// <summary>
    /// 异常类型全名
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// 异常消息
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// 堆栈
    /// </summary>
    public string? StackTrace { get; set; }

    /// <summary>
    /// 从异常实例创建快照
    /// </summary>
    /// <param name="exception">异常实例</param>
    /// <returns>审计用快照</returns>
    public static AuditLogException From(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return new AuditLogException
        {
            Type = exception.GetType().FullName,
            Message = exception.Message,
            StackTrace = exception.StackTrace,
        };
    }
}

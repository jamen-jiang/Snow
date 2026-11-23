namespace Snow.Auditing;

/// <summary>
/// 审计动作: 被审计方法的一次执行
/// </summary>
public sealed class AuditLogActionInfo
{
    /// <summary>
    /// 服务名(声明类型全名)
    /// </summary>
    public string? ServiceName { get; set; }

    /// <summary>
    /// 方法名
    /// </summary>
    public string? MethodName { get; set; }

    /// <summary>
    /// 参数(序列化后的文本)
    /// </summary>
    public string? Parameters { get; set; }

    /// <summary>
    /// 执行时间
    /// </summary>
    public DateTime ExecutionTime { get; set; }

    /// <summary>
    /// 执行耗时(毫秒)
    /// </summary>
    public long ExecutionDuration { get; set; }
}

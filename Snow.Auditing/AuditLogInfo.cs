namespace Snow.Auditing;

/// <summary>
/// 一条审计日志: 一次被审计调用的全貌
/// </summary>
public sealed class AuditLogInfo
{
    /// <summary>
    /// 日志Id
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// 应用名称
    /// </summary>
    public string? ApplicationName { get; set; }

    /// <summary>
    /// 调用方用户Id
    /// </summary>
    public long? UserId { get; set; }

    /// <summary>
    /// 调用方账号
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// 客户端IP
    /// </summary>
    public string? ClientIpAddress { get; set; }

    /// <summary>
    /// 客户端标识(User-Agent)
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// HTTP 方法
    /// </summary>
    public string? HttpMethod { get; set; }

    /// <summary>
    /// 请求地址
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// 响应状态码
    /// </summary>
    public int? HttpStatusCode { get; set; }

    /// <summary>
    /// 调用发生时间
    /// </summary>
    public DateTime ExecutionTime { get; set; }

    /// <summary>
    /// 调用总耗时(毫秒)
    /// </summary>
    public long ExecutionDuration { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Comments { get; set; }

    /// <summary>
    /// 被审计的动作(一次调用可包含多个)
    /// </summary>
    public List<AuditLogActionInfo> Actions { get; } = [];

    /// <summary>
    /// 调用过程中抛出的异常
    /// </summary>
    public List<AuditLogException> Exceptions { get; } = [];

    /// <summary>
    /// 扩展字段
    /// </summary>
    public Dictionary<string, object?> ExtraProperties { get; } = new(StringComparer.Ordinal);

    /// <summary>
    /// 本次调用是否出现了异常
    /// </summary>
    public bool HasException => Exceptions.Count > 0;
}

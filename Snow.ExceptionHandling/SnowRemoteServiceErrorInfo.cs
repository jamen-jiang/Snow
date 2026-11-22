namespace Snow.ExceptionHandling;

/// <summary>
/// 远端错误信息(对应 ABP 的 RemoteServiceErrorInfo)
/// </summary>
public sealed class SnowRemoteServiceErrorInfo
{
    /// <summary>
    /// 错误码, 客户端据此分支处理
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    /// 错误消息
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// 细节(堆栈等), 是否返回由 <see cref="SnowExceptionHandlingOptions.SendExceptionDetailsToClients"/> 控制
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// 字段级校验错误: 字段名 → 错误消息列表
    /// </summary>
    public Dictionary<string, string[]>? ValidationErrors { get; set; }

    /// <summary>
    /// 附加数据, 原样序列化给客户端
    /// </summary>
    public Dictionary<string, object?>? Data { get; set; }
}

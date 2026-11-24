namespace Snow.AspNetCore.ExceptionHandling;

/// <summary>
/// 异常处理选项
/// </summary>
public sealed class SnowExceptionHandlingOptions
{
    /// <summary>
    /// 是否把异常细节(异常类型与消息链)返回给客户端, 默认 false 只回"服务器内部错误"
    /// </summary>
    public bool SendExceptionDetailsToClients { get; set; }

    /// <summary>
    /// 是否把堆栈返回给客户端, 默认 false
    /// </summary>
    public bool SendStackTraceToClients { get; set; }
}

namespace Snow.AspNetCore.ExceptionHandling;

/// <summary>
/// 异常处理选项(对应 ABP 的 <c>AbpExceptionHandlingOptions</c>)
/// </summary>
public sealed class SnowExceptionHandlingOptions
{
    /// <summary>
    /// 是否把异常细节(异常类型与消息链)返回给客户端, 默认 false 只回"服务器内部错误"
    /// </summary>
    public bool SendExceptionDetailsToClients { get; set; }

    /// <summary>
    /// 与 ABP 同名属性一致的别名, 指向 <see cref="SendExceptionDetailsToClients"/>
    /// </summary>
    public bool SendExceptionsDetailsToClients
    {
        get => SendExceptionDetailsToClients;
        set => SendExceptionDetailsToClients = value;
    }

    /// <summary>
    /// 是否把堆栈返回给客户端, 默认 false
    /// </summary>
    public bool SendStackTraceToClients { get; set; }

    /// <summary>
    /// 允许把异常 Data 原样回传给客户端的异常类型(对应 ABP 的同名集合)
    /// </summary>
    public HashSet<Type> SendExceptionDataToClientTypes { get; set; } = new();

    /// <summary>
    /// 这条异常是否应该记日志(对应 ABP 的同名方法, 默认记录)
    /// </summary>
    /// <param name="exception">异常</param>
    public bool ShouldLogException(Exception exception)
    {
        _ = exception;
        return true;
    }
}

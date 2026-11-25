using Snow.Exceptions;

namespace Snow.AspNetCore.ExceptionHandling;

/// <summary>
/// 异常处理选项(对应 ABP 的 <c>AbpExceptionHandlingOptions</c>)
/// </summary>
public class SnowExceptionHandlingOptions
{
    /// <summary>
    /// 是否把异常细节(异常类型与消息链)返回给客户端
    /// </summary>
    public bool SendExceptionsDetailsToClients { get; set; }

    /// <summary>
    /// 是否把堆栈返回给客户端
    /// </summary>
    public bool SendStackTraceToClients { get; set; }

    /// <summary>
    /// 允许把异常 Data 原样回传给客户端的异常类型
    /// </summary>
    public List<Type> SendExceptionDataToClientTypes { get; set; }

    /// <summary>
    /// 把某些异常排除出日志的选择器: 任一返回 true 即不记录该异常
    /// </summary>
    public List<Func<Exception, bool>> ExcludeExceptionFromLoggerSelectors { get; }

    /// <summary>
    /// 构造器(默认值与 ABP 一致)
    /// </summary>
    public SnowExceptionHandlingOptions()
    {
        SendExceptionsDetailsToClients = false;
        SendStackTraceToClients = true;
        SendExceptionDataToClientTypes = new List<Type> { typeof(IBusinessException) };
        ExcludeExceptionFromLoggerSelectors = new List<Func<Exception, bool>>();
    }

    /// <summary>
    /// 这条异常是否应该记日志
    /// </summary>
    /// <param name="exception">异常</param>
    public bool ShouldLogException(Exception exception)
    {
        return ExcludeExceptionFromLoggerSelectors.All(selector => !selector(exception));
    }
}

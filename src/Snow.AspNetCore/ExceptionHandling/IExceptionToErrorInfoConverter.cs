namespace Snow.AspNetCore.ExceptionHandling;

/// <summary>
/// 异常 → 远端错误信息的转换器
/// </summary>
public interface IExceptionToErrorInfoConverter
{
    /// <summary>
    /// 转换异常
    /// </summary>
    /// <param name="exception">待转换的异常</param>
    /// <returns>统一的错误信息</returns>
    SnowRemoteServiceErrorInfo Convert(Exception exception);
}

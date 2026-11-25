using Snow.Http;

namespace Snow.AspNetCore.ExceptionHandling;

/// <summary>
/// 异常 → 远端错误信息的转换器(对应 ABP 的 <c>IExceptionToErrorInfoConverter</c>)
/// </summary>
public interface IExceptionToErrorInfoConverter
{
    /// <summary>
    /// 转换异常, 并在转换时用回调整理本次生效的选项(与 ABP 同形, 选项回调可省略)
    /// </summary>
    /// <param name="exception">待转换的异常</param>
    /// <param name="optionsAction">对本次转换生效的选项做微调</param>
    /// <returns>统一的错误信息</returns>
    RemoteServiceErrorInfo Convert(Exception exception, Action<SnowExceptionHandlingOptions>? optionsAction = null);
}

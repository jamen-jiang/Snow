using Snow.Localization;

namespace Snow.ExceptionHandling;

/// <summary>
/// 可本地化自身消息的异常(对应 ABP 的 <c>ILocalizeErrorMessage</c>)
/// </summary>
public interface ILocalizeErrorMessage
{
    /// <summary>
    /// 本地化异常消息
    /// </summary>
    /// <param name="context">本地化上下文</param>
    string LocalizeMessage(LocalizationContext context);
}

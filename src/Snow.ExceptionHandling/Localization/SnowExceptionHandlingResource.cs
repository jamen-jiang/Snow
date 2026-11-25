using Snow.Localization;

namespace Snow.ExceptionHandling.Localization;

/// <summary>
/// 异常处理内置文案的本地化资源(对应 ABP 的 <c>AbpExceptionHandlingResource</c>)
/// <para>资源名由 <see cref="LocalizationResourceNameAttribute"/> 指定为 <c>SnowExceptionHandling</c>; 通过
/// <c>IStringLocalizer&lt;SnowExceptionHandlingResource&gt;</c> 即可解析本资源。文案随包放在同目录的
/// <c>*.json</c>(en/zh-Hans/zh-Hant), 由 <c>SnowExceptionHandlingModule</c> 读取嵌入资源后注册。</para>
/// </summary>
[LocalizationResourceName("SnowExceptionHandling")]
public class SnowExceptionHandlingResource
{
}

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Snow.Validation;

/// <summary>
/// 带校验错误列表的异常契约(对应 ABP 的 <c>IHasValidationErrors</c>)
/// <para>异常处理层只看这个接口取字段级错误, 不关心具体异常类型, 于是第三方校验异常也能被统一翻译。</para>
/// </summary>
public interface IHasValidationErrors
{
    /// <summary>
    /// 校验错误列表, 每项含错误消息与涉及的成员名
    /// </summary>
    IReadOnlyList<ValidationResult> ValidationErrors { get; }
}

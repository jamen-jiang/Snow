using System.ComponentModel.DataAnnotations;

namespace Snow.Validation;

/// <summary>
/// 对象校验器: 用 DataAnnotations 特性校验一个对象及其嵌套对象
/// </summary>
public interface ISnowObjectValidator
{
    /// <summary>
    /// 校验对象
    /// </summary>
    /// <param name="instance">待校验对象</param>
    /// <returns>错误列表, 空表示通过</returns>
    IReadOnlyList<ValidationResult> Validate(object instance);

    /// <summary>
    /// 校验对象, 有错误时抛 <see cref="SnowValidationException"/>
    /// </summary>
    /// <param name="instance">待校验对象</param>
    void ValidateAndThrow(object instance);
}

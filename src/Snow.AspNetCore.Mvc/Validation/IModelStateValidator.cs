using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Snow.AspNetCore.Mvc.Validation;

/// <summary>
/// 模型状态校验器(对齐 ABP 的 <c>IModelStateValidator</c>)
/// <para>把 <see cref="ModelStateDictionary"/> 里的绑定错误收集为 <see cref="ValidationResult"/>, 供校验过滤器在参数校验前先检查。</para>
/// </summary>
public interface IModelStateValidator
{
    /// <summary>
    /// 校验模型状态, 有错误时抛出 <see cref="Snow.Validation.SnowValidationException"/>
    /// </summary>
    /// <param name="modelState">MVC 模型状态</param>
    void Validate(ModelStateDictionary modelState);

    /// <summary>
    /// 把模型状态里的错误追加到指定集合
    /// </summary>
    /// <param name="errors">错误收集器</param>
    /// <param name="modelState">MVC 模型状态</param>
    void AddErrors(ICollection<ValidationResult> errors, ModelStateDictionary modelState);
}
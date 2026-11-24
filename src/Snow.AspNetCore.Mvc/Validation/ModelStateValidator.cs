using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Snow.DependencyInjection;
using Snow.Validation;

namespace Snow.AspNetCore.Mvc.Validation;

/// <summary>
/// 默认模型状态校验器(对齐 ABP 的 <c>ModelStateValidator</c>)
/// <para>从 <see cref="ModelStateDictionary"/> 汇总绑定错误, 有错误时抛 <see cref="SnowValidationException"/>。</para>
/// </summary>
[TransientService]
public class ModelStateValidator : IModelStateValidator
{
    /// <inheritdoc />
    /// <param name="modelState">模型状态</param>
    public virtual void Validate(ModelStateDictionary modelState)
    {
        List<ValidationResult> errors = [];
        AddErrors(errors, modelState);

        if (errors.Count > 0)
        {
            throw new SnowValidationException(errors, "ModelState is not valid! See ValidationErrors for details.");
        }
    }

    /// <inheritdoc />
    /// <param name="errors">错误集合</param>
    /// <param name="modelState">模型状态</param>
    public virtual void AddErrors(ICollection<ValidationResult> errors, ModelStateDictionary modelState)
    {
        ArgumentNullException.ThrowIfNull(errors);
        ArgumentNullException.ThrowIfNull(modelState);

        if (modelState.IsValid)
        {
            return;
        }

        foreach (KeyValuePair<string, ModelStateEntry> entry in modelState)
        {
            if (entry.Value is null)
            {
                continue;
            }

            foreach (ModelError error in entry.Value.Errors)
            {
                errors.Add(new ValidationResult(error.ErrorMessage, new[] { entry.Key }));
            }
        }
    }
}
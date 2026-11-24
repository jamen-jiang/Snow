using System;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Snow.AspNetCore.Mvc.Validation;

/// <summary>
/// 校验特性辅助类(对齐 ABP 的 <c>ValidationAttributeHelper</c>)
/// <para>通过反射读取 <see cref="ValidationAttribute"/> 的默认错误消息, 用于在自定义本地化前补默认文案。</para>
/// </summary>
public static class ValidationAttributeHelper
{
    private readonly static PropertyInfo ValidationAttributeErrorMessageStringProperty = typeof(ValidationAttribute)
        .GetProperty("ErrorMessageString", BindingFlags.Instance | BindingFlags.NonPublic)!;

    private readonly static PropertyInfo ValidationAttributeCustomErrorMessageSetProperty = typeof(ValidationAttribute)
        .GetProperty("CustomErrorMessageSet", BindingFlags.Instance | BindingFlags.NonPublic)!;

    /// <summary>
    /// 给校验特性设置默认错误消息(未显式设置时)
    /// </summary>
    /// <param name="validationAttribute">校验特性</param>
    public static void SetDefaultErrorMessage(ValidationAttribute validationAttribute)
    {
        if (validationAttribute is StringLengthAttribute stringLength && stringLength.MinimumLength != 0)
        {
            var customErrorMessageSet = ValidationAttributeCustomErrorMessageSetProperty.GetValue(validationAttribute) as bool?;
            if (customErrorMessageSet != true)
            {
                validationAttribute.ErrorMessage =
                    "The field {0} must be a string with a minimum length of {2} and a maximum length of {1}.";
                return;
            }
        }

        try
        {
            var errorMessageString = ValidationAttributeErrorMessageStringProperty.GetValue(validationAttribute) as string;
            validationAttribute.ErrorMessage = errorMessageString;
        }
        catch (Exception)
        {
            // ignored
        }
    }
}
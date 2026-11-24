using System;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Threading.Tasks;
using Snow.DependencyInjection;
using Snow.Reflection;

namespace Snow.Validation;

/// <summary>
/// 方法参数校验器(对应 ABP 的 <c>MethodInvocationValidator</c>): 校验方法各参数值, 有错抛 <see cref="SnowValidationException"/>
/// </summary>
[TransientService]
public class MethodInvocationValidator : IMethodInvocationValidator
{
    private readonly ISnowObjectValidator _objectValidator;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="objectValidator">对象校验器</param>
    public MethodInvocationValidator(ISnowObjectValidator objectValidator)
    {
        _objectValidator = objectValidator;
    }

    /// <inheritdoc />
    /// <param name="context">上下文</param>
    public virtual Task ValidateAsync(MethodInvocationValidationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Parameters.Length == 0)
        {
            return Task.CompletedTask;
        }

        if (!context.Method.IsPublic)
        {
            return Task.CompletedTask;
        }

        if (IsValidationDisabled(context))
        {
            return Task.CompletedTask;
        }

        if (context.Parameters.Length != context.ParameterValues.Length)
        {
            throw new Exception("方法参数数量与实参数量不一致!");
        }

        // 单个 null 参数的边界情况: 直接抛校验错误
        if (context.Errors.Count > 0 && HasSingleNullArgument(context))
        {
            ThrowValidationError(context);
        }

        AddMethodParameterValidationErrors(context);

        if (context.Errors.Count > 0)
        {
            ThrowValidationError(context);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// 判断方法是否禁用了校验(方法或其声明类型上标了 <see cref="DisableValidationAttribute"/>)
    /// </summary>
    /// <param name="context">方法调用校验上下文</param>
    /// <returns>禁用返回 true</returns>
    protected virtual bool IsValidationDisabled(MethodInvocationValidationContext context)
    {
        return ReflectionHelper.GetSingleAttributeOfMemberOrDeclaringTypeOrDefault<DisableValidationAttribute>(context.Method) != null;
    }

    /// <summary>
    /// 判断是否只有一个 null 实参
    /// </summary>
    /// <param name="context">方法调用校验上下文</param>
    /// <returns>只有一个 null 实参返回 true</returns>
    protected virtual bool HasSingleNullArgument(MethodInvocationValidationContext context)
    {
        return context.Parameters.Length == 1 && context.ParameterValues[0] == null;
    }

    /// <summary>
    /// 抛出参数校验异常
    /// </summary>
    /// <param name="context">方法调用校验上下文</param>
    protected virtual void ThrowValidationError(MethodInvocationValidationContext context)
    {
        throw new SnowValidationException(context.Errors, "方法参数校验失败! 详见 ValidationErrors。");
    }

    /// <summary>
    /// 收集所有参数的校验错误
    /// </summary>
    /// <param name="context">方法调用校验上下文</param>
    protected virtual void AddMethodParameterValidationErrors(MethodInvocationValidationContext context)
    {
        for (int i = 0; i < context.Parameters.Length; i++)
        {
            AddMethodParameterValidationErrors(context, context.Parameters[i], context.ParameterValues[i]);
        }
    }

    /// <summary>
    /// 收集单个参数的校验错误
    /// </summary>
    /// <param name="context">方法调用校验上下文</param>
    /// <param name="parameterInfo">参数定义</param>
    /// <param name="parameterValue">参数值</param>
    protected virtual void AddMethodParameterValidationErrors(MethodInvocationValidationContext context, ParameterInfo parameterInfo, object? parameterValue)
    {
        bool allowNulls = parameterInfo.IsOptional
            || parameterInfo.IsOut
            || TypeHelper.IsNullable(parameterInfo.ParameterType)
            || SnowValidationHelper.IsSimpleType(parameterInfo.ParameterType);

        if (parameterValue is null)
        {
            if (allowNulls)
            {
                return;
            }

            context.Errors.Add(new ValidationResult($"参数 \"{parameterInfo.Name}\" 不能为空。", new[] { parameterInfo.Name ?? string.Empty }));
            return;
        }

        if (SnowValidationHelper.IsSimpleType(parameterInfo.ParameterType))
        {
            return;
        }

        context.Errors.AddRange(_objectValidator.Validate(parameterValue));
    }
}

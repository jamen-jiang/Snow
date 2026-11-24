using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Snow.Validation;

/// <summary>
/// 方法调用校验上下文(对应 ABP 的 <c>MethodInvocationValidationContext</c>)
/// </summary>
public class MethodInvocationValidationContext
{
    /// <summary>
    /// 目标对象
    /// </summary>
    public object? TargetObject { get; }

    /// <summary>
    /// 被校验的方法
    /// </summary>
    public MethodInfo Method { get; }

    /// <summary>
    /// 实参值
    /// </summary>
    public object?[] ParameterValues { get; }

    /// <summary>
    /// 方法参数定义
    /// </summary>
    public ParameterInfo[] Parameters { get; }

    /// <summary>
    /// 校验错误集合
    /// </summary>
    public List<ValidationResult> Errors { get; } = [];

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="targetObject">目标对象</param>
    /// <param name="method">被校验的方法</param>
    /// <param name="parameterValues">实参值</param>
    public MethodInvocationValidationContext(object? targetObject, MethodInfo method, object?[] parameterValues)
    {
        TargetObject = targetObject;
        Method = method;
        ParameterValues = parameterValues;
        Parameters = method.GetParameters();
    }
}

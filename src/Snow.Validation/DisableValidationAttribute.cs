using System;

namespace Snow.Validation;

/// <summary>
/// 标在方法/类/属性上, 关闭自动参数校验(对应 ABP 的 <c>DisableValidationAttribute</c>)
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Property)]
public class DisableValidationAttribute : Attribute
{
}

namespace Snow.Validation;

/// <summary>
/// 参数校验标记(对应 ABP 的校验开关): 贴在类或方法上, 校验每个复杂参数, 有错误直接抛 <see cref="SnowValidationException"/>
/// <para>简单类型参数(CancellationToken / 值类型 / 字符串等)不校验; 嵌套对象按选项递归。</para>
/// <para>本特性只是元数据, 干活的是 <see cref="SnowValidationInterceptor"/>, 由
/// <see cref="SnowValidationInterceptorRegistrar"/> 按它的存在挂上去; MVC 侧的
/// <c>SnowValidationActionFilter</c> 也按它决定是否校验 Action 参数。</para>
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class ValidationAttribute : Attribute
{
}

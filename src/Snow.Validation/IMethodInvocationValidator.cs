using System.Threading.Tasks;

namespace Snow.Validation;

/// <summary>
/// 方法调用参数校验器(对应 ABP 的 <c>IMethodInvocationValidator</c>)
/// </summary>
public interface IMethodInvocationValidator
{
    /// <summary>
    /// 异步校验方法参数
    /// </summary>
    /// <param name="context">方法调用校验上下文</param>
    Task ValidateAsync(MethodInvocationValidationContext context);
}

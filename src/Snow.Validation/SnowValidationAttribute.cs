using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Snow.Aop;

namespace Snow.Validation;

/// <summary>
/// 方法级参数校验拦截器: 校验每个复杂参数, 有错误直接抛 <see cref="SnowValidationException"/>
/// <para>简单类型参数(CancellationToken / 值类型 / 字符串等)不校验; 嵌套对象按选项递归。</para>
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class SnowValidationAttribute : SnowInterceptorAttribute
{
    /// <inheritdoc />
    public override async Task InvokeAsync(AspectContext context, AspectDelegate next, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        SnowValidationOptions options = context.ServiceProvider
            .GetRequiredService<IOptions<SnowValidationOptions>>()
            .Value;

        if (options.Enabled)
        {
            ISnowObjectValidator validator = context.ServiceProvider.GetRequiredService<ISnowObjectValidator>();
            List<ValidationResult> errors = new();

            foreach (object? argument in context.Arguments)
            {
                if (argument is null || argument is CancellationToken || SnowValidationHelper.IsSimpleType(argument.GetType()))
                {
                    continue;
                }

                errors.AddRange(validator.Validate(argument));
            }

            if (errors.Count > 0)
            {
                throw new SnowValidationException(errors);
            }
        }

        await next(context, cancellationToken).ConfigureAwait(false);
    }
}

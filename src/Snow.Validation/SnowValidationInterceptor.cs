using System.ComponentModel.DataAnnotations;
using Snow.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Snow.DynamicProxy;

namespace Snow.Validation;

/// <summary>
/// 参数校验拦截器: 校验每个复杂参数, 有错误直接抛 <see cref="SnowValidationException"/>
/// </summary>
[TransientService]
public sealed class SnowValidationInterceptor : SnowInterceptor
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="serviceProvider">服务提供者</param>
    public SnowValidationInterceptor(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public override async Task InterceptAsync(ISnowMethodInvocation invocation)
    {
        ArgumentNullException.ThrowIfNull(invocation);

        SnowValidationOptions options = _serviceProvider
            .GetRequiredService<IOptions<SnowValidationOptions>>()
            .Value;

        if (options.Enabled)
        {
            ISnowObjectValidator validator = _serviceProvider.GetRequiredService<ISnowObjectValidator>();
            List<ValidationResult> errors = new();

            foreach (object? argument in invocation.Arguments)
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

        await invocation.ProceedAsync().ConfigureAwait(false);
    }
}

using FluentValidation;
using ValidationResult = System.ComponentModel.DataAnnotations.ValidationResult;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Snow.Validation.FluentValidation;

/// <summary>
/// 校验管线: 先跑 DataAnnotations 特性, 再跑 FluentValidation 校验器, 错误合成一份
/// <para>这是本框架里 <see cref="ISnowObjectValidator"/> 的最终实现, 所以 [SnowValidation] 拦截器
/// 与应用代码显式调用校验器都会同时覆盖两套规则, 不需要改调用处。</para>
/// </summary>
public sealed class FluentValidationObjectValidator : ISnowObjectValidator
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly DataAnnotationObjectValidator _dataAnnotations;
    private readonly SnowFluentValidationOptions _options;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="scopeFactory">作用域工厂: 校验器可能是 Scoped, 必须从新作用域里取</param>
    /// <param name="dataAnnotations">DataAnnotations 校验器</param>
    /// <param name="options">FluentValidation 集成选项</param>
    public FluentValidationObjectValidator(
        IServiceScopeFactory scopeFactory,
        DataAnnotationObjectValidator dataAnnotations,
        IOptions<SnowFluentValidationOptions> options)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _dataAnnotations = dataAnnotations ?? throw new ArgumentNullException(nameof(dataAnnotations));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    /// <param name="instance">实例</param>
    public IReadOnlyList<ValidationResult> Validate(object instance)
    {
        ArgumentNullException.ThrowIfNull(instance);

        List<ValidationResult> errors = [];

        if (_options.ValidateDataAnnotations)
        {
            errors.AddRange(_dataAnnotations.Validate(instance));
        }

        errors.AddRange(ValidateWithFluentValidation(instance));
        return errors;
    }

    /// <inheritdoc />
    /// <param name="instance">实例</param>
    public void ValidateAndThrow(object instance)
    {
        IReadOnlyList<ValidationResult> errors = Validate(instance);

        if (errors.Count > 0)
        {
            throw new SnowValidationException(errors);
        }
    }

    private IEnumerable<ValidationResult> ValidateWithFluentValidation(object instance)
    {
        Type validatorType = typeof(IValidator<>).MakeGenericType(instance.GetType());

        // 本对象是单例, 而校验器默认按 Scoped 登记: 直接取会报「无法从根提供程序解析 Scoped 服务」,
        // 所以每次校验开一个作用域, 校验器就算依赖 DbContext 之类的作用域服务也能取到
        using IServiceScope scope = _scopeFactory.CreateScope();

        if (scope.ServiceProvider.GetService(validatorType) is not IValidator validator)
        {
            return [];
        }

        // 校验器是 IValidator<T>, 而上下文要用真实类型构造, 只能反射建
        Type contextType = typeof(ValidationContext<>).MakeGenericType(instance.GetType());
        IValidationContext context = (IValidationContext)Activator.CreateInstance(contextType, instance)!;
        global::FluentValidation.Results.ValidationResult result = validator.Validate(context);

        return result.Errors.Select(failure => new ValidationResult(
            failure.ErrorMessage,
            string.IsNullOrWhiteSpace(failure.PropertyName) ? [] : [failure.PropertyName])).ToList();
    }
}

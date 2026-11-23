using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.Extensions.Options;
using Snow.DependencyInjection;

namespace Snow.Validation;

/// <summary>
/// 基于 DataAnnotations 的对象校验器
/// </summary>
[SingletonService]
public sealed class DataAnnotationObjectValidator : ISnowObjectValidator
{
    private readonly IOptions<SnowValidationOptions> _options;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="options">校验选项</param>
    public DataAnnotationObjectValidator(IOptions<SnowValidationOptions> options) => _options = options;

    /// <inheritdoc />
    public IReadOnlyList<ValidationResult> Validate(object instance)
    {
        ArgumentNullException.ThrowIfNull(instance);

        List<ValidationResult> results = new();
        ValidateInternal(instance, results, new HashSet<object>(ReferenceEqualityComparer.Instance), 0);
        return results;
    }

    /// <inheritdoc />
    public void ValidateAndThrow(object instance)
    {
        IReadOnlyList<ValidationResult> errors = Validate(instance);
        if (errors.Count > 0)
        {
            throw new SnowValidationException(errors);
        }
    }

    private void ValidateInternal(object instance, List<ValidationResult> results, HashSet<object> visited, int depth)
    {
        SnowValidationOptions options = _options.Value;
        if (!options.Enabled || depth > options.MaxDepth)
        {
            return;
        }

        if (!visited.Add(instance))
        {
            return;
        }

        ValidationContext context = new(instance);
        Validator.TryValidateObject(instance, context, results, validateAllProperties: true);

        if (!options.ValidateNestedObjects)
        {
            return;
        }

        foreach (PropertyInfo property in instance.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.GetIndexParameters().Length > 0 || !property.CanRead)
            {
                continue;
            }

            object? value = property.GetValue(instance);
            if (value is null || IsSimpleType(value.GetType()))
            {
                continue;
            }

            if (value is IEnumerable enumerable and not string)
            {
                foreach (object? item in enumerable)
                {
                    if (item is not null && !IsSimpleType(item.GetType()))
                    {
                        ValidateInternal(item, results, visited, depth + 1);
                    }
                }

                continue;
            }

            ValidateInternal(value, results, visited, depth + 1);
        }
    }

    private static bool IsSimpleType(Type type)
    {
        Type underlying = Nullable.GetUnderlyingType(type) ?? type;

        return underlying.IsPrimitive
            || underlying.IsEnum
            || underlying == typeof(string)
            || underlying == typeof(decimal)
            || underlying == typeof(Guid)
            || underlying == typeof(DateTime)
            || underlying == typeof(DateTimeOffset)
            || underlying == typeof(DateOnly)
            || underlying == typeof(TimeOnly)
            || underlying == typeof(TimeSpan)
            || underlying == typeof(Uri)
            || typeof(IEnumerable).IsAssignableFrom(underlying);
    }
}

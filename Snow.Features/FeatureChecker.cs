using System.ComponentModel;
using System.Globalization;
using Snow.DependencyInjection;

namespace Snow.Features;

/// <summary>
/// 功能开关检查入口的默认实现: 按提供器优先级取第一个非 null 值
/// </summary>
[ScopedService]
public sealed class FeatureChecker : IFeatureChecker
{
    private readonly FeatureDefinitionManager _definitions;
    private readonly IReadOnlyList<IFeatureValueProvider> _providers;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="definitions">功能定义管理器</param>
    /// <param name="providers">全部值提供器, 按名排序后使用</param>
    public FeatureChecker(FeatureDefinitionManager definitions, IEnumerable<IFeatureValueProvider> providers)
    {
        _definitions = definitions;
        _providers = providers.OrderBy(provider => provider.Name, StringComparer.Ordinal).ToArray();
    }

    /// <inheritdoc />
    public async Task<bool> IsEnabledAsync(string name, CancellationToken cancellationToken = default)
        => await GetValueAsync<bool>(name, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<string?> GetOrNullAsync(string name, CancellationToken cancellationToken = default)
    {
        FeatureDefinition? definition = _definitions.GetOrNull(name);
        if (definition is null)
        {
            return null;
        }

        foreach (IFeatureValueProvider provider in _providers)
        {
            string? value = await provider.GetOrNullAsync(definition, cancellationToken).ConfigureAwait(false);
            if (value is not null)
            {
                return value;
            }
        }

        return definition.DefaultValue;
    }

    /// <inheritdoc />
    public async Task<T?> GetValueAsync<T>(string name, CancellationToken cancellationToken = default)
    {
        string? value = await GetOrNullAsync(name, cancellationToken).ConfigureAwait(false);
        if (value is null)
        {
            return default;
        }

        object? converted = ConvertValue(value, typeof(T));
        return converted is null ? default : (T)converted;
    }

    private static object? ConvertValue(string value, Type type)
    {
        Type targetType = Nullable.GetUnderlyingType(type) ?? type;

        if (targetType == typeof(string))
        {
            return value;
        }

        if (targetType == typeof(bool))
        {
            return bool.TryParse(value, out bool parsed) ? parsed : value is "1" or "yes" or "on" ? true : value is "0" or "no" or "off" ? false : throw new FormatException("无法把功能值 '" + value + "' 当成布尔值");
        }

        if (targetType.IsEnum)
        {
            return Enum.Parse(targetType, value, ignoreCase: true);
        }

        TypeConverter converter = TypeDescriptor.GetConverter(targetType);
        if (converter.CanConvertFrom(typeof(string)))
        {
            return converter.ConvertFromInvariantString(value);
        }

        return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
    }
}

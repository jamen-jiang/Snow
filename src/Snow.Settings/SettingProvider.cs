using System.ComponentModel;
using System.Globalization;
using Snow.DependencyInjection;

namespace Snow.Settings;

/// <summary>
/// 设置读取入口的默认实现: 按提供器优先级取第一个非 null 值
/// <para>优先级 = <see cref="ISettingValueProviderManager.Providers"/> 反过来遍历(列表靠后的更高),
/// 与 ABP 的 <c>Enumerable.Reverse(SettingValueProviderManager.Providers)</c> 一致。</para>
/// </summary>
[ScopedService]
public sealed class SettingProvider : ISettingProvider
{
    private readonly SettingDefinitionManager _definitions;
    private readonly IReadOnlyList<ISettingValueProvider> _providers;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="definitions">设置定义管理器</param>
    /// <param name="providers">值提供器管理器</param>
    public SettingProvider(SettingDefinitionManager definitions, ISettingValueProviderManager providers)
    {
        _definitions = definitions;
        _providers = providers.Providers.Reverse().ToArray();
    }

    /// <inheritdoc />
    public async Task<string?> GetOrNullAsync(string name, CancellationToken cancellationToken = default)
    {
        SettingDefinition? definition = _definitions.GetOrNull(name);
        if (definition is null)
        {
            return null;
        }

        foreach (ISettingValueProvider provider in _providers)
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
    public async Task<T?> GetAsync<T>(string name, CancellationToken cancellationToken = default)
    {
        string? value = await GetOrNullAsync(name, cancellationToken).ConfigureAwait(false);
        if (value is null)
        {
            return default;
        }

        object? converted = ConvertValue(value, typeof(T));
        return converted is null ? default : (T)converted;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SettingValue>> GetAllAsync(CancellationToken cancellationToken = default)
        => await GetAllAsync(_definitions.GetAll().Select(definition => definition.Name).ToArray(), cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<SettingValue>> GetAllAsync(string[] names, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(names);

        List<SettingValue> values = new(names.Length);
        foreach (string name in names)
        {
            values.Add(new SettingValue(name, await GetOrNullAsync(name, cancellationToken).ConfigureAwait(false)));
        }

        return values;
    }

    private static object? ConvertValue(string value, Type type)
    {
        Type targetType = Nullable.GetUnderlyingType(type) ?? type;

        if (targetType == typeof(string))
        {
            return value;
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

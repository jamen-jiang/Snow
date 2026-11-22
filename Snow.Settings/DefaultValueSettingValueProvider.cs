using Snow.DependencyInjection;

namespace Snow.Settings;

/// <summary>
/// 默认值提供器: 返回定义上的默认值, 优先级最低(名字 "D" 排序靠后)
/// </summary>
[SingletonService]
public sealed class DefaultValueSettingValueProvider : ISettingValueProvider
{
    /// <inheritdoc />
    public string Name => "D";

    /// <inheritdoc />
    public Task<string?> GetOrNullAsync(SettingDefinition definition, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(definition);
        return Task.FromResult(definition.DefaultValue);
    }
}

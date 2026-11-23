namespace Snow.Settings;

/// <summary>
/// 设置定义上下文的默认实现, 按名字典存储
/// </summary>
public sealed class SettingDefinitionContext : ISettingDefinitionContext
{
    private readonly Dictionary<string, SettingDefinition> _definitions = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public void Add(SettingDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        _definitions[definition.Name] = definition;
    }

    /// <inheritdoc />
    public SettingDefinition? GetOrNull(string name)
        => _definitions.TryGetValue(name, out SettingDefinition? definition) ? definition : null;

    /// <summary>
    /// 取出全部定义, 供定义管理器汇总
    /// </summary>
    /// <returns>定义集合</returns>
    public IReadOnlyCollection<SettingDefinition> GetAll() => _definitions.Values;
}

using Snow.Exceptions;

namespace Snow.Settings;

/// <summary>
/// <see cref="ISettingDefinitionContext"/> 的默认实现: 一个按名字索引的字典
/// </summary>
public class SettingDefinitionContext : ISettingDefinitionContext
{
    private readonly Dictionary<string, SettingDefinition> _definitions = new();

    /// <summary>
    /// 已登记的定义(按名字索引)
    /// </summary>
    public IReadOnlyDictionary<string, SettingDefinition> Definitions => _definitions;

    /// <inheritdoc />
    public SettingDefinition? GetOrNull(string name)
    {
        return _definitions.GetValueOrDefault(name);
    }

    /// <inheritdoc />
    public void Add(params SettingDefinition[] definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);

        foreach (SettingDefinition definition in definitions)
        {
            if (_definitions.ContainsKey(definition.Name))
            {
                throw new SnowException("该配置项已被定义过: " + definition.Name);
            }

            _definitions[definition.Name] = definition;
        }
    }

    /// <summary>
    /// 删除一个定义(用于 <see cref="SnowSettingOptions.DeletedSettings"/>)
    /// </summary>
    /// <param name="name">配置项名</param>
    public void Remove(string name)
    {
        _definitions.Remove(name);
    }
}

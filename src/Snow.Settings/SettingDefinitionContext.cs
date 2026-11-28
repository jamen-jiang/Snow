using Snow.Exceptions;

namespace Snow.Settings;

/// <summary>
/// 配置项定义上下文: 收集各模块声明的 <see cref="SettingDefinition"/>
/// </summary>
public sealed class SettingDefinitionContext
{
    private readonly Dictionary<string, SettingDefinition> _definitions = new();

    /// <summary>已声明的配置项</summary>
    public IReadOnlyDictionary<string, SettingDefinition> Definitions => _definitions;

    /// <summary>按名称取得定义; 不存在时返回 null</summary>
    public SettingDefinition? GetOrNull(string name) => _definitions.GetValueOrDefault(name);

    /// <summary>添加定义; 重名时抛出异常</summary>
    public void Add(params SettingDefinition[] definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);

        foreach (SettingDefinition definition in definitions)
        {
            if (!_definitions.TryAdd(definition.Name, definition))
            {
                throw new SnowException("该配置项已被定义过: " + definition.Name);
            }
        }
    }

    /// <summary>移除定义</summary>
    public void Remove(string name) => _definitions.Remove(name);
}

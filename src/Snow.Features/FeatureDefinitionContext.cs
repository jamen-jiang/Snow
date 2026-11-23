namespace Snow.Features;

/// <summary>
/// 功能定义上下文的默认实现, 按名字典存储
/// </summary>
public sealed class FeatureDefinitionContext : IFeatureDefinitionContext
{
    private readonly Dictionary<string, FeatureDefinition> _definitions = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public void Add(FeatureDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        _definitions[definition.Name] = definition;
    }

    /// <inheritdoc />
    public FeatureDefinition? GetOrNull(string name)
        => _definitions.TryGetValue(name, out FeatureDefinition? definition) ? definition : null;

    /// <summary>
    /// 取出全部定义, 供定义管理器汇总
    /// </summary>
    /// <returns>定义集合</returns>
    public IReadOnlyCollection<FeatureDefinition> GetAll() => _definitions.Values;
}

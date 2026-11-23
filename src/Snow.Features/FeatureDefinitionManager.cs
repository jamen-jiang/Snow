using System.Collections.Concurrent;
using Snow.DependencyInjection;

namespace Snow.Features;

/// <summary>
/// 功能定义管理器: 汇总所有 <see cref="FeatureDefinitionProvider"/> 的定义并缓存
/// </summary>
[SingletonService]
public sealed class FeatureDefinitionManager
{
    private readonly IEnumerable<FeatureDefinitionProvider> _providers;
    private readonly ConcurrentDictionary<string, FeatureDefinition> _definitions = new(StringComparer.Ordinal);
    private volatile bool _initialized;
    private readonly object _sync = new();

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="providers">全部功能定义提供器</param>
    public FeatureDefinitionManager(IEnumerable<FeatureDefinitionProvider> providers) => _providers = providers;

    /// <summary>
    /// 按名取定义
    /// </summary>
    /// <param name="name">功能名</param>
    /// <returns>定义, 不存在为 null</returns>
    public FeatureDefinition? GetOrNull(string name)
    {
        EnsureInitialized();
        return _definitions.TryGetValue(name, out FeatureDefinition? definition) ? definition : null;
    }

    /// <summary>
    /// 按名取定义, 不存在抛异常
    /// </summary>
    /// <param name="name">功能名</param>
    /// <returns>定义</returns>
    public FeatureDefinition Get(string name)
        => GetOrNull(name) ?? throw new KeyNotFoundException("未定义的功能: " + name);

    /// <summary>
    /// 取全部定义
    /// </summary>
    /// <returns>定义集合</returns>
    public IReadOnlyCollection<FeatureDefinition> GetAll()
    {
        EnsureInitialized();
        return _definitions.Values.ToArray();
    }

    private void EnsureInitialized()
    {
        if (_initialized)
        {
            return;
        }

        lock (_sync)
        {
            if (_initialized)
            {
                return;
            }

            FeatureDefinitionContext context = new();
            foreach (FeatureDefinitionProvider provider in _providers)
            {
                provider.Define(context);
            }

            foreach (FeatureDefinition definition in context.GetAll())
            {
                _definitions[definition.Name] = definition;
            }

            _initialized = true;
        }
    }
}

using System.Collections.Concurrent;
using Snow.DependencyInjection;

namespace Snow.Settings;

/// <summary>
/// 设置定义管理器: 汇总所有 <see cref="SettingDefinitionProvider"/> 的定义并缓存
/// </summary>
[SingletonService]
public sealed class SettingDefinitionManager
{
    private readonly IEnumerable<SettingDefinitionProvider> _providers;
    private readonly ConcurrentDictionary<string, SettingDefinition> _definitions = new(StringComparer.Ordinal);
    private volatile bool _initialized;
    private readonly object _sync = new();

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="providers">全部设置定义提供器</param>
    public SettingDefinitionManager(IEnumerable<SettingDefinitionProvider> providers) => _providers = providers;

    /// <summary>
    /// 按名取定义
    /// </summary>
    /// <param name="name">设置名</param>
    /// <returns>定义, 不存在为 null</returns>
    public SettingDefinition? GetOrNull(string name)
    {
        EnsureInitialized();
        return _definitions.TryGetValue(name, out SettingDefinition? definition) ? definition : null;
    }

    /// <summary>
    /// 按名取定义, 不存在抛异常
    /// </summary>
    /// <param name="name">设置名</param>
    /// <returns>定义</returns>
    public SettingDefinition Get(string name)
        => GetOrNull(name) ?? throw new KeyNotFoundException("未定义的设置: " + name);

    /// <summary>
    /// 取全部定义
    /// </summary>
    /// <returns>定义集合</returns>
    public IReadOnlyCollection<SettingDefinition> GetAll()
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

            SettingDefinitionContext context = new();
            foreach (SettingDefinitionProvider provider in _providers)
            {
                provider.Define(context);
            }

            foreach (SettingDefinition definition in context.GetAll())
            {
                _definitions[definition.Name] = definition;
            }

            _initialized = true;
        }
    }
}

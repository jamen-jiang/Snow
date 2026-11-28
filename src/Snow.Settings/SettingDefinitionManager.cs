using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Snow.DependencyInjection;

namespace Snow.Settings;

/// <summary>
/// 汇总各模块的 <see cref="SettingDefinitionProvider"/>, 首次读取时构建定义表
/// </summary>
[SingletonService]
internal sealed class SettingDefinitionManager
{
    private readonly SnowSettingOptions _options;
    private readonly IServiceProvider _serviceProvider;
    private readonly object _syncObject = new();
    private IReadOnlyDictionary<string, SettingDefinition>? _definitions;

    public SettingDefinitionManager(IOptions<SnowSettingOptions> options, IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(serviceProvider);

        _options = options.Value;
        _serviceProvider = serviceProvider;
    }

    /// <summary>按名称取得定义; 不存在时返回 null</summary>
    public SettingDefinition? GetOrNull(string name) => Definitions.GetValueOrDefault(name);

    private IReadOnlyDictionary<string, SettingDefinition> Definitions
    {
        get
        {
            if (_definitions is not null)
            {
                return _definitions;
            }

            lock (_syncObject)
            {
                return _definitions ??= CreateDefinitions();
            }
        }
    }

    private IReadOnlyDictionary<string, SettingDefinition> CreateDefinitions()
    {
        SettingDefinitionContext context = new();

        foreach (Type providerType in _options.DefinitionProviders)
        {
            SettingDefinitionProvider provider =
                (SettingDefinitionProvider)ActivatorUtilities.CreateInstance(_serviceProvider, providerType);
            provider.Define(context);
        }

        foreach (string deletedSetting in _options.DeletedSettings)
        {
            context.Remove(deletedSetting);
        }

        return context.Definitions;
    }
}

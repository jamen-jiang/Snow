using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Snow.DependencyInjection;
using Snow.Exceptions;

namespace Snow.Settings;

/// <summary>
/// <see cref="ISettingDefinitionManager"/> 的默认实现: 首次访问时按 <see cref="SnowSettingOptions.DefinitionProviders"/>
/// 依次执行 <see cref="SettingDefinitionProvider.Define"/>, 再扣掉 <see cref="SnowSettingOptions.DeletedSettings"/>
/// </summary>
[SingletonService]
public class SettingDefinitionManager : ISettingDefinitionManager
{
    private readonly SnowSettingOptions _options;
    private readonly IServiceProvider _serviceProvider;
    private readonly object _syncObject = new();

    private IReadOnlyDictionary<string, SettingDefinition>? _definitions;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="options">配置项选项</param>
    /// <param name="serviceProvider">服务提供器, 用来创建定义提供器</param>
    public SettingDefinitionManager(IOptions<SnowSettingOptions> options, IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(serviceProvider);

        _options = options.Value;
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public SettingDefinition Get(string name)
    {
        return GetOrNull(name) ?? throw new SnowException("未知的配置项: " + name);
    }

    /// <inheritdoc />
    public SettingDefinition? GetOrNull(string name)
    {
        return Definitions.GetValueOrDefault(name);
    }

    /// <inheritdoc />
    public IReadOnlyList<SettingDefinition> GetAll()
    {
        return Definitions.Values.ToList();
    }

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

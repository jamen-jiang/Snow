using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Snow.DependencyInjection;

namespace Snow.Settings;

/// <summary>
/// <see cref="ISettingValueProviderManager"/> 的默认实现: 按 <see cref="SnowSettingOptions.ValueProviders"/> 建实例, 只建一次
/// </summary>
[SingletonService]
public class SettingValueProviderManager : ISettingValueProviderManager
{
    private readonly SnowSettingOptions _options;
    private readonly IServiceProvider _serviceProvider;
    private readonly object _syncObject = new();

    private List<ISettingValueProvider>? _providers;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="options">配置项选项</param>
    /// <param name="serviceProvider">服务提供器</param>
    public SettingValueProviderManager(IOptions<SnowSettingOptions> options, IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(serviceProvider);

        _options = options.Value;
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public IReadOnlyList<ISettingValueProvider> Providers
    {
        get
        {
            if (_providers is not null)
            {
                return _providers;
            }

            lock (_syncObject)
            {
                return _providers ??= CreateProviders();
            }
        }
    }

    private List<ISettingValueProvider> CreateProviders()
    {
        return _options.ValueProviders
            .Select(type => (ISettingValueProvider)ActivatorUtilities.CreateInstance(_serviceProvider, type))
            .ToList();
    }
}

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Snow.DependencyInjection;
using Snow.Modularity;

namespace Snow.Settings;

/// <summary>
/// 值提供器管理器的默认实现: 从 <see cref="SnowSettingOptions"/> 取类型再向容器要实例
/// <para>同名提供器视为配置错误直接抛出, 免得优先级静默变成随机。</para>
/// </summary>
[SingletonService]
public sealed class SettingValueProviderManager : ISettingValueProviderManager
{
    private readonly Lazy<IReadOnlyList<ISettingValueProvider>> _providers;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="serviceProvider">服务提供者</param>
    /// <param name="options">设置选项</param>
    public SettingValueProviderManager(IServiceProvider serviceProvider, IOptions<SnowSettingOptions> options)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(options);

        SnowSettingOptions settingOptions = options.Value;
        _providers = new Lazy<IReadOnlyList<ISettingValueProvider>>(
            () => settingOptions.ValueProviders
                .Select(type => (ISettingValueProvider)serviceProvider.GetRequiredService(type))
                .ToList(),
            isThreadSafe: true);
    }

    /// <inheritdoc />
    public IReadOnlyList<ISettingValueProvider> Providers
    {
        get
        {
            IReadOnlyList<ISettingValueProvider> providers = _providers.Value;
            IGrouping<string, ISettingValueProvider>? duplicated = providers.GroupBy(provider => provider.Name).FirstOrDefault(group => group.Count() > 1);
            if (duplicated is not null)
            {
                throw new SnowInitializationException(
                    "设置值提供器重名: " + duplicated.Key + "。实现类: " +
                    string.Join(", ", duplicated.Select(provider => provider.GetType().FullName)));
            }

            return providers;
        }
    }
}

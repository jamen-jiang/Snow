using Microsoft.Extensions.Configuration;
using Snow.DependencyInjection;

namespace Snow.Settings;

/// <summary>
/// 从 <see cref="IConfiguration"/> 取值的来源(对应 ABP 的 ConfigurationSettingValueProvider):
/// 配置项的键是 <c>Settings:{配置项名}</c>
/// <para>例如 <c>Snow.Mailing.Smtp.Host</c> 在 appsettings.json 里写成
/// <c>"Settings": { "Snow.Mailing.Smtp.Host": "127.0.0.1" }</c>。</para>
/// </summary>
[SingletonService]
public class ConfigurationSettingValueProvider : ISettingValueProvider
{
    /// <summary>
    /// 配置键前缀
    /// </summary>
    public const string ConfigurationNamePrefix = "Settings:";

    /// <summary>
    /// 来源名
    /// </summary>
    public const string ProviderName = "C";

    private readonly IConfiguration _configuration;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="configuration">配置</param>
    public ConfigurationSettingValueProvider(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        _configuration = configuration;
    }

    /// <inheritdoc />
    public string Name => ProviderName;

    /// <inheritdoc />
    public Task<string?> GetOrNullAsync(SettingDefinition setting)
    {
        ArgumentNullException.ThrowIfNull(setting);

        return Task.FromResult(_configuration[ConfigurationNamePrefix + setting.Name]);
    }
}

using Microsoft.Extensions.Configuration;
using Snow.DependencyInjection;

namespace Snow.Settings;

/// <summary>
/// 默认配置项读取器: 配置值优先于定义默认值, 加密值在返回前解密
/// </summary>
[TransientService]
internal sealed class SettingProvider : ISettingProvider
{
    private const string ConfigurationNamePrefix = "Settings:";

    private readonly SettingDefinitionManager _definitionManager;
    private readonly SettingEncryptionService _encryptionService;
    private readonly IConfiguration _configuration;

    public SettingProvider(
        SettingDefinitionManager definitionManager,
        SettingEncryptionService encryptionService,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(definitionManager);
        ArgumentNullException.ThrowIfNull(encryptionService);
        ArgumentNullException.ThrowIfNull(configuration);

        _definitionManager = definitionManager;
        _encryptionService = encryptionService;
        _configuration = configuration;
    }

    /// <inheritdoc />
    public Task<string?> GetOrNullAsync(string name)
    {
        SettingDefinition? setting = _definitionManager.GetOrNull(name);
        if (setting is null)
        {
            return Task.FromResult<string?>(null);
        }

        string? value = _configuration[ConfigurationNamePrefix + setting.Name] ?? setting.DefaultValue;
        if (setting.IsEncrypted)
        {
            value = _encryptionService.Decrypt(setting, value);
        }

        return Task.FromResult(value);
    }
}

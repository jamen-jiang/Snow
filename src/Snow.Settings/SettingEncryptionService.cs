using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Snow.DependencyInjection;
using Snow.Security.Encryption;

namespace Snow.Settings;

/// <summary>
/// 配置值解密器: 解密失败时按选项决定返回原文还是空串
/// </summary>
[TransientService]
internal sealed class SettingEncryptionService
{
    private readonly IStringEncryptionService _encryptionService;
    private readonly SnowSettingOptions _options;
    private readonly ILogger<SettingEncryptionService> _logger;

    public SettingEncryptionService(
        IStringEncryptionService encryptionService,
        IOptions<SnowSettingOptions> options,
        ILogger<SettingEncryptionService> logger)
    {
        ArgumentNullException.ThrowIfNull(encryptionService);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _encryptionService = encryptionService;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>解密配置值; null 直接返回</summary>
    public string? Decrypt(SettingDefinition setting, string? cipherValue)
    {
        ArgumentNullException.ThrowIfNull(setting);

        if (cipherValue is null)
        {
            return null;
        }

        try
        {
            return _encryptionService.Decrypt(cipherValue);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "配置项 {SettingName} 的值解密失败, 它不是用当前口令加密的。",
                setting.Name);

            return _options.ReturnOriginalValueIfDecryptFailed ? cipherValue : string.Empty;
        }
    }
}

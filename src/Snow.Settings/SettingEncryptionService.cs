using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Snow.DependencyInjection;
using Snow.Security.Encryption;

namespace Snow.Settings;

/// <summary>
/// <see cref="ISettingEncryptionService"/> 的默认实现: 交给 <see cref="IStringEncryptionService"/>;
/// 解密失败时按 <see cref="SnowSettingOptions.ReturnOriginalValueIfDecryptFailed"/> 决定返回原文还是空串
/// </summary>
[TransientService]
public class SettingEncryptionService : ISettingEncryptionService
{
    private readonly IStringEncryptionService _encryptionService;
    private readonly SnowSettingOptions _options;
    private readonly ILogger<SettingEncryptionService> _logger;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="encryptionService">文本加解密</param>
    /// <param name="options">配置项选项</param>
    /// <param name="logger">日志</param>
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

    /// <inheritdoc />
    public string? Encrypt(SettingDefinition setting, string? plainValue)
    {
        ArgumentNullException.ThrowIfNull(setting);

        if (plainValue is null)
        {
            return null;
        }

        return _encryptionService.Encrypt(plainValue);
    }

    /// <inheritdoc />
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

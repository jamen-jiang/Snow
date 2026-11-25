using Snow.DependencyInjection;
using Snow.Exceptions;

namespace Snow.Settings;

/// <summary>
/// <see cref="ISettingProvider"/> 的默认实现(对应 ABP 的 SettingProvider):
/// 定义 → 倒序问各取质来源 → 首个非 null 值获胜 → 加密项解密
/// </summary>
[TransientService]
public class SettingProvider : ISettingProvider
{
    private readonly ISettingDefinitionManager _definitionManager;
    private readonly ISettingEncryptionService _encryptionService;
    private readonly ISettingValueProviderManager _valueProviderManager;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="definitionManager">定义表</param>
    /// <param name="encryptionService">加解密</param>
    /// <param name="valueProviderManager">取值来源</param>
    public SettingProvider(
        ISettingDefinitionManager definitionManager,
        ISettingEncryptionService encryptionService,
        ISettingValueProviderManager valueProviderManager)
    {
        ArgumentNullException.ThrowIfNull(definitionManager);
        ArgumentNullException.ThrowIfNull(encryptionService);
        ArgumentNullException.ThrowIfNull(valueProviderManager);

        _definitionManager = definitionManager;
        _encryptionService = encryptionService;
        _valueProviderManager = valueProviderManager;
    }

    /// <inheritdoc />
    public async Task<string?> GetOrNullAsync(string name)
    {
        SettingDefinition? setting = _definitionManager.GetOrNull(name);
        if (setting is null)
        {
            return null;
        }

        string? value = await GetOrNullValueAsync(setting).ConfigureAwait(false);
        if (setting.IsEncrypted)
        {
            value = _encryptionService.Decrypt(setting, value);
        }

        return value;
    }

    /// <inheritdoc />
    public async Task<string> GetAsync(string name)
    {
        return await GetOrNullAsync(name).ConfigureAwait(false) ?? throw new SnowException("配置项没有值: " + name);
    }

    /// <inheritdoc />
    public async Task<List<SettingValue>> GetAllAsync(string[] names)
    {
        ArgumentNullException.ThrowIfNull(names);

        List<SettingValue> values = new(names.Length);
        foreach (string name in names)
        {
            values.Add(new SettingValue(name, await GetOrNullAsync(name).ConfigureAwait(false)));
        }

        return values;
    }

    /// <inheritdoc />
    public Task<List<SettingValue>> GetAllAsync()
    {
        return GetAllAsync(_definitionManager.GetAll().Select(setting => setting.Name).ToArray());
    }

    private async Task<string?> GetOrNullValueAsync(SettingDefinition setting)
    {
        // 倒序: 后登记的来源优先(与 ABP 一致), 便于宿主覆盖框架默认值
        for (int i = _valueProviderManager.Providers.Count - 1; i >= 0; i--)
        {
            ISettingValueProvider provider = _valueProviderManager.Providers[i];

            if (setting.Providers.Count > 0 && !setting.Providers.Contains(provider.Name))
            {
                continue;
            }

            string? value = await provider.GetOrNullAsync(setting).ConfigureAwait(false);
            if (value is not null)
            {
                return value;
            }
        }

        return setting.DefaultValue;
    }
}

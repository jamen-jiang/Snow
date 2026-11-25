namespace Snow.Settings;

/// <summary>
/// 配置项加解密(对应 ABP 的 ISettingEncryptionService): 只处理 <see cref="SettingDefinition.IsEncrypted"/> 的定义
/// </summary>
public interface ISettingEncryptionService
{
    /// <summary>
    /// 加密一个值
    /// </summary>
    /// <param name="setting">配置项定义</param>
    /// <param name="plainValue">明文</param>
    /// <returns>密文; 明文为 null 时返回 null</returns>
    string? Encrypt(SettingDefinition setting, string? plainValue);

    /// <summary>
    /// 解密一个值
    /// </summary>
    /// <param name="setting">配置项定义</param>
    /// <param name="cipherValue">密文</param>
    /// <returns>明文; 密文为 null 时返回 null</returns>
    string? Decrypt(SettingDefinition setting, string? cipherValue);
}

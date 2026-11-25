namespace Snow.Security.Encryption;

/// <summary>
/// 简单文本加解密(AES + PBKDF2 派生密钥), 对应 ABP 的 IStringEncryptionService
/// <para>用于加密配置里不该以明文出现的值(例如 SMTP 密码)。</para>
/// </summary>
public interface IStringEncryptionService
{
    /// <summary>
    /// 加密文本
    /// </summary>
    /// <param name="plainText">明文; null 返回 null</param>
    /// <param name="passPhrase">口令, 不给用 <see cref="SnowStringEncryptionOptions.DefaultPassPhrase"/></param>
    /// <param name="salt">盐, 不给用 <see cref="SnowStringEncryptionOptions.DefaultSalt"/></param>
    /// <returns>密文的 Base64 表示</returns>
    string? Encrypt(string? plainText, string? passPhrase = null, byte[]? salt = null);

    /// <summary>
    /// 解密 <see cref="Encrypt"/> 产出的密文
    /// </summary>
    /// <param name="cipherText">Base64 密文; 空值返回 null</param>
    /// <param name="passPhrase">口令, 不给用默认口令</param>
    /// <param name="salt">盐, 不给用默认盐</param>
    /// <returns>明文</returns>
    string? Decrypt(string? cipherText, string? passPhrase = null, byte[]? salt = null);
}

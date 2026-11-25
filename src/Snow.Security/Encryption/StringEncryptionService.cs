using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Snow.DependencyInjection;

namespace Snow.Security.Encryption;

/// <summary>
/// <see cref="IStringEncryptionService"/> 的默认实现: PBKDF2 派生密钥 + AES/CBC/PKCS7
/// <para>与 ABP 的 StringEncryptionService 算法一致(默认口令、盐、迭代次数与哈希算法都相同),
/// 同样的明文在两边产出同样的密文。</para>
/// </summary>
[TransientService]
public sealed class StringEncryptionService : IStringEncryptionService
{
    private readonly SnowStringEncryptionOptions _options;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="options">加密参数</param>
    public StringEncryptionService(IOptions<SnowStringEncryptionOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options.Value;
    }

    /// <inheritdoc />
    public string? Encrypt(string? plainText, string? passPhrase = null, byte[]? salt = null)
    {
        if (plainText is null)
        {
            return null;
        }

        using Aes aes = Aes.Create();
        aes.Mode = CipherMode.CBC;

        using ICryptoTransform encryptor = aes.CreateEncryptor(DeriveKey(passPhrase, salt), _options.InitVectorBytes);
        using MemoryStream output = new();
        using (CryptoStream cryptoStream = new(output, encryptor, CryptoStreamMode.Write))
        {
            byte[] bytes = Encoding.UTF8.GetBytes(plainText);
            cryptoStream.Write(bytes, 0, bytes.Length);
            cryptoStream.FlushFinalBlock();
        }

        return Convert.ToBase64String(output.ToArray());
    }

    /// <inheritdoc />
    public string? Decrypt(string? cipherText, string? passPhrase = null, byte[]? salt = null)
    {
        if (cipherText.IsNullOrEmpty())
        {
            return null;
        }

        byte[] fullCipher = Convert.FromBase64String(cipherText);

        using Aes aes = Aes.Create();
        aes.Mode = CipherMode.CBC;

        using ICryptoTransform decryptor = aes.CreateDecryptor(DeriveKey(passPhrase, salt), _options.InitVectorBytes);
        using MemoryStream input = new(fullCipher);
        using CryptoStream cryptoStream = new(input, decryptor, CryptoStreamMode.Read);
        using MemoryStream output = new();
        cryptoStream.CopyTo(output);

        return Encoding.UTF8.GetString(output.ToArray());
    }

    private byte[] DeriveKey(string? passPhrase, byte[]? salt)
    {
        // 与 ABP 相同: PBKDF2-SHA1、1000 次迭代
        return Rfc2898DeriveBytes.Pbkdf2(
            passPhrase ?? _options.DefaultPassPhrase,
            salt ?? _options.DefaultSalt,
            1000,
            HashAlgorithmName.SHA1,
            _options.Keysize / 8);
    }
}

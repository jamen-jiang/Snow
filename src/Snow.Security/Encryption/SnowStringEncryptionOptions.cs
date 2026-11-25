using System.Text;

namespace Snow.Security.Encryption;

/// <summary>
/// <see cref="IStringEncryptionService"/> 用到的参数(对应 ABP 的 AbpStringEncryptionOptions)
/// <para>默认口令只用于开发期开箱可用, 生产环境应当换掉 <see cref="DefaultPassPhrase"/>。</para>
/// </summary>
public class SnowStringEncryptionOptions
{
    /// <summary>
    /// 密钥长度(位), 默认 256
    /// </summary>
    public int Keysize { get; set; } = 256;

    /// <summary>
    /// 默认口令; 口令经 PBKDF2 派生密钥, 改动它等于换密钥, 已加密的旧值将无法解密
    /// </summary>
    public string DefaultPassPhrase { get; set; } = "gsKnGZ041HLL4IM8";

    /// <summary>
    /// 初始向量(16 字节)
    /// </summary>
    public byte[] InitVectorBytes { get; set; } = Encoding.ASCII.GetBytes("jkE49230Tf093b42");

    /// <summary>
    /// 派生密钥用的盐
    /// </summary>
    public byte[] DefaultSalt { get; set; } = Encoding.ASCII.GetBytes("hgt!16kl");
}

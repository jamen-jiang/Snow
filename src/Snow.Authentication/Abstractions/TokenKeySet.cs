using Microsoft.IdentityModel.Tokens;

namespace Snow.Authentication;

/// <summary>
/// 解析好的密钥: 签发凭据 + 校验密钥 + 允许的算法
/// </summary>
public sealed class TokenKeySet
{
    /// <summary>
    /// 签名算法: HMAC + SHA-256
    /// <para>放在这里而不是密钥解析器上: 签发凭据、校验密钥、允许的算法三处都引用它, 换算法只改这一处。</para>
    /// </summary>
    public const string Algorithm = SecurityAlgorithms.HmacSha256;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="key">签名与验签共用的对称密钥</param>
    internal TokenKeySet(SymmetricSecurityKey key)
    {
        Key = key;
        SigningCredentials = new SigningCredentials(key, Algorithm);
    }

    /// <summary>
    /// 签名与验签共用的对称密钥
    /// </summary>
    public SymmetricSecurityKey Key { get; }

    /// <summary>
    /// 签发凭据
    /// </summary>
    public SigningCredentials SigningCredentials { get; }

    /// <summary>
    /// 校验用密钥
    /// </summary>
    public IReadOnlyList<SecurityKey> ValidationKeys => [Key];

    /// <summary>
    /// 允许的算法: 只有 HS256
    /// <para>校验时钉死这一个值, 否则有算法混淆攻击 —— 攻击者把令牌头改成别的算法再签一份,
    /// 有些实现会验过。</para>
    /// </summary>
    public IReadOnlyList<string> ValidAlgorithms { get; } = [Algorithm];
}

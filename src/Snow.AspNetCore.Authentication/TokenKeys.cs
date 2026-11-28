using System.Text;
using Microsoft.IdentityModel.Tokens;
using Snow.Security.Claims;

namespace Snow.AspNetCore.Authentication;

/// <summary>
/// 密钥与校验参数: 签发与验签共用同一套口径, 只有配置实例变化时才重建
/// </summary>
internal static class TokenKeys
{
    /// <summary>
    /// 签名算法: HMAC + SHA-256
    /// </summary>
    public const string Algorithm = SecurityAlgorithms.HmacSha256;

    /// <summary>
    /// 允许的算法: 只有 HS256, 钉死以防算法混淆攻击
    /// </summary>
    public static readonly string[] ValidAlgorithms = [Algorithm];

    // 对称密钥不公开指纹当 kid: 那等于把密钥的哈希写进令牌头
    private const string KeyId = "default";

    public static SymmetricSecurityKey CreateKey(SnowAuthenticationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        int bytes = Encoding.UTF8.GetByteCount(options.SecretKey);
        if (bytes < 32)
        {
            throw new InvalidOperationException($"Authentication:SecretKey 太短: HS256 要求至少 32 字节, 当前 {bytes} 字节。");
        }

        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SecretKey)) { KeyId = KeyId };
    }

    public static TokenValidationParameters CreateValidationParameters(SnowAuthenticationOptions options, SecurityKey key)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(key);

        return new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = options.GetIssuer(),
            ValidateAudience = true,
            ValidAudience = options.GetAudience(),
            // 关掉框架的有效期校验: 令牌里的 exp 只是签发时的快照, 真正判过期的是服务端会话(会滑动续期)
            ValidateLifetime = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKeys = [key],
            ValidAlgorithms = ValidAlgorithms,
            NameClaimType = SnowClaimTypes.UserName,
            RoleClaimType = SnowClaimTypes.Role,
        };
    }
}

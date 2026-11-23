using System.Text;
using Microsoft.IdentityModel.Tokens;
using Snow.DependencyInjection;

namespace Snow.Authentication;

/// <summary>
/// 解析好的密钥: 签发凭据 + 校验密钥 + 允许的算法
/// </summary>
public sealed class TokenKeySet
{
    internal TokenKeySet(SymmetricSecurityKey key)
    {
        Key = key;
        SigningCredentials = new SigningCredentials(key, OptionsTokenKeyProvider.Algorithm);
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
    public IReadOnlyList<string> ValidAlgorithms { get; } = [OptionsTokenKeyProvider.Algorithm];
}

/// <summary>
/// 把配置翻译成密钥
/// <para>换成从 KeyVault / 环境变量 / KMS 取密钥, 实现这个接口即可, 其余代码不用动。</para>
/// </summary>
public interface ITokenKeyProvider
{
    /// <summary>
    /// 解析密钥; 配置有错直接抛 <see cref="InvalidOperationException"/>, 在启动阶段就暴露
    /// </summary>
    TokenKeySet Resolve(SnowAuthenticationOptions options);
}

/// <summary>
/// 默认实现: 从 <see cref="SnowAuthenticationOptions.SecretKey"/> 读对称密钥
/// <para>HS256 的密钥就是一串字节, 解析没有开销, 但仍然按选项实例缓存: 热更新换了实例才重建。</para>
/// </summary>
[SingletonService]
public sealed class OptionsTokenKeyProvider : ITokenKeyProvider
{
    /// <summary>
    /// 签名算法: HMAC + SHA-256
    /// </summary>
    public const string Algorithm = SecurityAlgorithms.HmacSha256;

    // 对称密钥不公开指纹当 kid: 那等于把密钥的哈希写进令牌头
    private const string KeyId = "default";

    private SnowAuthenticationOptions? _cachedOptions;
    private TokenKeySet? _cached;

    /// <inheritdoc />
    public TokenKeySet Resolve(SnowAuthenticationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        TokenKeySet? cached = _cached;
        if (cached is not null && ReferenceEquals(_cachedOptions, options))
        {
            return cached;
        }

        TokenKeySet fresh = Build(options);
        _cachedOptions = options;
        _cached = fresh;
        return fresh;
    }

    private static TokenKeySet Build(SnowAuthenticationOptions options)
    {
        if (Encoding.UTF8.GetByteCount(options.SecretKey) < 32)
        {
            throw new InvalidOperationException("Authentication:SecretKey 太短: HS256 要求至少 32 字节。");
        }

        return new TokenKeySet(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SecretKey)) { KeyId = KeyId });
    }
}

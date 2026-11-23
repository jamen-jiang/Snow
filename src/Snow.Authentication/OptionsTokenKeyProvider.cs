using System.Text;
using Microsoft.IdentityModel.Tokens;
using Snow.DependencyInjection;

namespace Snow.Authentication;

/// <summary>
/// 默认实现: 从 <see cref="SnowAuthenticationOptions.SecretKey"/> 读对称密钥
/// <para>HS256 的密钥就是一串字节, 解析没有开销, 但仍然按选项实例缓存: 热更新换了实例才重建。</para>
/// </summary>
[SingletonService]
public sealed class OptionsTokenKeyProvider : ITokenKeyProvider
{
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

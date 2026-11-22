using System.Text;

namespace Snow.Authentication;

/// <summary>
/// 认证配置: 一节 <c>Authentication</c>
/// <para>令牌由本包用 <c>SecretKey</c> 以 HS256 签发, 校验时用同一把密钥验签, 再查一次服务端会话。</para>
/// <para>只有 <see cref="SecretKey"/> 必填; <see cref="Issuer"/> 与 <see cref="Audience"/> 留空按 <c>"Snow"</c> 处理,
/// <see cref="Expiration"/> 留空按 24 小时。本包自签自验, 这两项没有安全作用, 只影响令牌长什么样。</para>
/// </summary>
public sealed class SnowAuthenticationOptions
{
    /// <summary>
    /// HS256 对称密钥: 签发与校验共用这一把, 至少 32 字节
    /// <para>用 <c>openssl rand -base64 48</c> 生成。它既是签名密钥也是验签密钥, 泄露即可伪造任意用户令牌,
    /// 所以别提交进仓库: 用环境变量、user-secrets 或密钥管理服务注入。</para>
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// 签发者: 写进令牌的 <c>iss</c>, 校验时也必须等于它。留空就是 <c>"Snow"</c>
    /// <para>本包签发、本包校验, 这一项没有安全作用, 只是让令牌符合 RFC 7519 的常规形态, 也方便将来接入别的服务时区分来源。</para>
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// 受众: 写进令牌的 <c>aud</c>, 校验时比对。留空就是 <c>"Snow"</c>
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// 滑动窗口, 默认 24 小时: 持续使用就一直有效, 停用超过一个窗口才需要重新登录
    /// </summary>
    public TimeSpan Expiration { get; set; } = TimeSpan.FromHours(24);

    /// <summary>
    /// 取签发者, 去掉首尾空白与尾部斜杠: 少数校验方对尾部斜杠敏感, 不去掉会永远对不上
    /// </summary>
    public string GetIssuer() => string.IsNullOrWhiteSpace(Issuer) ? DefaultValue : Issuer.Trim().TrimEnd('/');

    /// <summary>
    /// 取受众, 去掉首尾空白
    /// </summary>
    public string GetAudience() => string.IsNullOrWhiteSpace(Audience) ? DefaultValue : Audience.Trim();

    /// <summary>
    /// <see cref="Issuer"/> 与 <see cref="Audience"/> 都留空时用的默认值
    /// </summary>
    private const string DefaultValue = "Snow";

    /// <summary>
    /// 校验配置。写错就抛, 应用起不来, 而不是等第一个请求进来才发现全是 401
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(SecretKey))
        {
            throw new InvalidOperationException("Authentication:SecretKey 不能为空: 用 openssl rand -base64 48 生成一个至少 32 字节的密钥。");
        }

        int bytes = Encoding.UTF8.GetByteCount(SecretKey);
        if (bytes < 32)
        {
            throw new InvalidOperationException($"Authentication:SecretKey 太短: HS256 要求至少 32 字节, 当前 {bytes} 字节。");
        }

        if (Expiration <= TimeSpan.Zero)
        {
            throw new InvalidOperationException("Authentication:Expiration 必须大于 0。");
        }
    }
}

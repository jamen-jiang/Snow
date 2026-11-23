using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Snow.Authentication;

/// <summary>
/// 令牌解析: 只从 <c>Authorization</c> 请求头取 <c>Bearer &lt;token&gt;</c>(RFC 6750)
/// <para>不支持 Cookie 与查询参数: 两者都会把令牌写进访问日志、浏览器历史与 Referer,
/// 而且 Cookie 来源必须再处理 CSRF, 收益抵不上风险。</para>
/// </summary>
public static class TokenResolver
{
    /// <summary>
    /// 令牌所在的请求头
    /// </summary>
    public const string HeaderName = "Authorization";

    /// <summary>
    /// 认证方案名
    /// </summary>
    public const string Scheme = "Bearer";

    private const string Prefix = Scheme + " ";

    /// <summary>
    /// 从当前请求解析令牌
    /// </summary>
    /// <param name="context">HTTP 上下文</param>
    /// <returns>令牌, 未携带或格式不符返回 null</returns>
    public static string? ResolveToken(HttpContext? context)
    {
        if (context is null)
        {
            return null;
        }

        // 多值请求头只取第一个: 逗号拼接会把两个令牌连成一个坏令牌
        StringValues raw = context.Request.Headers[HeaderName];
        if (raw.Count == 0)
        {
            return null;
        }

        string value = raw[0]?.Trim() ?? string.Empty;
        if (value.Length == 0)
        {
            return null;
        }

        if (!value.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        string token = value[Prefix.Length..].Trim();
        return token.Length == 0 ? null : token;
    }
}

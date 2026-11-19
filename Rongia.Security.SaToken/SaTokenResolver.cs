using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Rongia.Security.SaToken;

/// <summary>
/// 令牌解析: 按 请求头 → Cookie → 查询参数 的顺序取令牌并去前缀
/// </summary>
public static class SaTokenResolver
{
    /// <summary>
    /// 从当前请求解析令牌
    /// </summary>
    /// <param name="context">HTTP 上下文</param>
    /// <param name="options">登录态配置</param>
    /// <returns>令牌, 未携带返回 null</returns>
    public static string? ResolveToken(HttpContext? context, SaTokenOptions options)
    {
        if (context is null || options is null)
        {
            return null;
        }

        StringValues raw = context.Request.Headers[options.TokenName];
        if (StringValues.IsNullOrEmpty(raw) && options.IsReadCookie)
        {
            raw = context.Request.Cookies[options.TokenName];
        }

        if (StringValues.IsNullOrEmpty(raw) && options.IsReadQuery)
        {
            raw = context.Request.Query[options.TokenName];
        }

        if (StringValues.IsNullOrEmpty(raw))
        {
            return null;
        }

        string value = raw.ToString().Trim();
        if (string.IsNullOrEmpty(options.TokenPrefix))
        {
            return value;
        }

        return value.StartsWith(options.TokenPrefix, StringComparison.OrdinalIgnoreCase)
            ? value[options.TokenPrefix.Length..].Trim()
            : null;
    }
}

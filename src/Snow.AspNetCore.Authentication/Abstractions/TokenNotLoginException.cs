using Snow.Exceptions;

namespace Snow.AspNetCore.Authentication;

/// <summary>
/// 未登录(或登录态已失效)异常: 由全局异常中间件按 401 回给调用方
/// </summary>
public sealed class TokenNotLoginException : TokenException, IHasHttpStatusCode
{
    /// <summary>
    /// 构造器
    /// </summary>
    public TokenNotLoginException(string? message = null) : base(message ?? "未登录或登录已过期。") { }

    /// <inheritdoc />
    public int HttpStatusCode => 401;
}

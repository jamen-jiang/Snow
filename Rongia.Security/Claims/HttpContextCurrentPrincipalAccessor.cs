using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Rongia.Security.Claims;

/// <summary>
/// 基于 <see cref="IHttpContextAccessor"/> 的主体访问器: Web 宿主下当前主体就是 <c>HttpContext.User</c>
/// <para>对齐 ABP 的 <c>HttpContextCurrentPrincipalAccessor</c>(它继承 ThreadCurrentPrincipalAccessor, 拿不到上下文时回落)</para>
/// </summary>
public sealed class HttpContextCurrentPrincipalAccessor : ThreadCurrentPrincipalAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="httpContextAccessor">HTTP 上下文访问器</param>
    public HttpContextCurrentPrincipalAccessor(IHttpContextAccessor httpContextAccessor)
    {
        ArgumentNullException.ThrowIfNull(httpContextAccessor);
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    protected override ClaimsPrincipal GetClaimsPrincipal()
    {
        return _httpContextAccessor.HttpContext?.User ?? base.GetClaimsPrincipal();
    }
}

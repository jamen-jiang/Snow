using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Snow.DependencyInjection;
using Snow.Security.Claims;

namespace Snow.AspNetCore.Security.Claims;

/// <summary>
/// 基于 <see cref="IHttpContextAccessor"/> 的主体访问器: Web 宿主下当前主体就是 <c>HttpContext.User</c>
/// <para>对齐 ABP 的 <c>HttpContextCurrentPrincipalAccessor</c>(它继承 ThreadCurrentPrincipalAccessor, 拿不到上下文时回落)</para>
/// <para>标 <c>ReplaceServices = true</c>: 约定注册的替换阶段会先清掉基类 <see cref="ThreadCurrentPrincipalAccessor"/> 登记的那一条,
/// 于是 <see cref="ICurrentPrincipalAccessor"/> 只剩 Web 实现这一条, 不需要模块里再写一次 Replace。</para>
/// <para>放在 Web 包而不是 Snow.Security: HTTP 上下文只有 ASP.NET Core 宿主才有, 与 ABP 把它放在 <c>Volo.Abp.AspNetCore</c> 同理。</para>
/// </summary>
[SingletonService(ReplaceServices = true)]
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

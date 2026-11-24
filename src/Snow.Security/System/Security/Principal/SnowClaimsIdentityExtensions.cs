using System.Linq;
using System.Security.Claims;
using Snow.Security.Claims;

namespace System.Security.Principal;

/// <summary>
/// <see cref="ClaimsIdentity"/> 与 <see cref="ClaimsPrincipal"/> 上的声明读取扩展(对应 ABP 的 <c>AbpClaimsIdentityExtensions</c>)
/// <para>ABP 把身份与主体两套重载放在同一个 <c>AbpClaimsIdentityExtensions</c> 类里, 这里保持同一形状, 类名按 Snow 命名改为 <c>SnowClaimsIdentityExtensions</c>。</para>
/// <para>与 ABP 的差异: 用户Id 用 <see cref="long"/> 而非 <see cref="Guid"/>(与 <c>ICurrentUser.Id</c> 全链路一致), 因此取值用的是 TryParse 而不是 Guid.Parse。</para>
/// </summary>
public static class SnowClaimsIdentityExtensions
{
    /// <summary>
    /// 取用户Id
    /// </summary>
    /// <param name="identity">身份</param>
    /// <returns>用户Id, 缺失或非数字为 null</returns>
    public static long? FindUserId(this ClaimsIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(identity);

        return ParseUserId(identity.FindClaimValue(SnowClaimTypes.UserId));
    }

    /// <summary>
    /// 取用户Id(在主体的全部身份里找)
    /// </summary>
    /// <param name="principal">主体</param>
    /// <returns>用户Id, 缺失或非数字为 null</returns>
    public static long? FindUserId(this ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        return ParseUserId(principal.FindClaimValue(SnowClaimTypes.UserId));
    }

    /// <summary>
    /// 取登录账号
    /// </summary>
    /// <param name="identity">身份</param>
    /// <returns>账号, 不存在为 null</returns>
    public static string? FindUserName(this ClaimsIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(identity);

        return identity.FindClaimValue(SnowClaimTypes.UserName);
    }

    /// <summary>
    /// 取登录账号(在主体的全部身份里找)
    /// </summary>
    /// <param name="principal">主体</param>
    /// <returns>账号, 不存在为 null</returns>
    public static string? FindUserName(this ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        return principal.FindClaimValue(SnowClaimTypes.UserName);
    }

    /// <summary>
    /// 取用户名称
    /// </summary>
    /// <param name="identity">身份</param>
    /// <returns>名称, 不存在为 null</returns>
    public static string? FindName(this ClaimsIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(identity);

        return identity.FindClaimValue(SnowClaimTypes.Name);
    }

    /// <summary>
    /// 取用户名称(在主体的全部身份里找)
    /// </summary>
    /// <param name="principal">主体</param>
    /// <returns>名称, 不存在为 null</returns>
    public static string? FindName(this ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        return principal.FindClaimValue(SnowClaimTypes.Name);
    }

    /// <summary>
    /// 取姓
    /// </summary>
    /// <param name="identity">身份</param>
    /// <returns>姓, 不存在为 null</returns>
    public static string? FindSurName(this ClaimsIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(identity);

        return identity.FindClaimValue(SnowClaimTypes.SurName);
    }

    /// <summary>
    /// 取姓(在主体的全部身份里找)
    /// </summary>
    /// <param name="principal">主体</param>
    /// <returns>姓, 不存在为 null</returns>
    public static string? FindSurName(this ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        return principal.FindClaimValue(SnowClaimTypes.SurName);
    }

    /// <summary>
    /// 取邮箱
    /// </summary>
    /// <param name="identity">身份</param>
    /// <returns>邮箱, 不存在为 null</returns>
    public static string? FindEmail(this ClaimsIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(identity);

        return identity.FindClaimValue(SnowClaimTypes.Email);
    }

    /// <summary>
    /// 取邮箱(在主体的全部身份里找)
    /// </summary>
    /// <param name="principal">主体</param>
    /// <returns>邮箱, 不存在为 null</returns>
    public static string? FindEmail(this ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        return principal.FindClaimValue(SnowClaimTypes.Email);
    }

    /// <summary>
    /// 取手机号
    /// </summary>
    /// <param name="identity">身份</param>
    /// <returns>手机号, 不存在为 null</returns>
    public static string? FindPhoneNumber(this ClaimsIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(identity);

        return identity.FindClaimValue(SnowClaimTypes.MobilePhone);
    }

    /// <summary>
    /// 取手机号(在主体的全部身份里找)
    /// </summary>
    /// <param name="principal">主体</param>
    /// <returns>手机号, 不存在为 null</returns>
    public static string? FindPhoneNumber(this ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        return principal.FindClaimValue(SnowClaimTypes.MobilePhone);
    }

    /// <summary>
    /// 取所属租户 id
    /// </summary>
    /// <param name="identity">身份</param>
    /// <returns>租户 id, 缺失或非 Guid 为 null(宿主用户为 null)</returns>
    public static Guid? FindTenantId(this ClaimsIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(identity);

        return ParseTenantId(identity.FindClaimValue(SnowClaimTypes.TenantId));
    }

    /// <summary>
    /// 取所属租户 id(在主体的全部身份里找)
    /// </summary>
    /// <param name="principal">主体</param>
    /// <returns>租户 id, 缺失或非 Guid 为 null(宿主用户为 null)</returns>
    public static Guid? FindTenantId(this ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        return ParseTenantId(principal.FindClaimValue(SnowClaimTypes.TenantId));
    }

    /// <summary>
    /// 取指定类型的第一条声明
    /// </summary>
    /// <param name="identity">身份</param>
    /// <param name="claimType">声明类型</param>
    /// <returns>声明, 不存在为 null</returns>
    public static Claim? FindClaim(this ClaimsIdentity identity, string claimType)
    {
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentNullException.ThrowIfNull(claimType);

        return identity.Claims.FirstOrDefault(claim => claim.Type == claimType);
    }

    /// <summary>
    /// 取指定类型的第一条声明(在主体的全部身份里找)
    /// </summary>
    /// <param name="principal">主体</param>
    /// <param name="claimType">声明类型</param>
    /// <returns>声明, 不存在为 null</returns>
    public static Claim? FindClaim(this ClaimsPrincipal principal, string claimType)
    {
        ArgumentNullException.ThrowIfNull(principal);
        ArgumentNullException.ThrowIfNull(claimType);

        return principal.Claims.FirstOrDefault(claim => claim.Type == claimType);
    }

    /// <summary>
    /// 取指定类型的第一条声明值
    /// </summary>
    /// <param name="identity">身份</param>
    /// <param name="claimType">声明类型</param>
    /// <returns>声明值, 不存在为 null</returns>
    public static string? FindClaimValue(this ClaimsIdentity identity, string claimType)
    {
        return identity.FindClaim(claimType)?.Value;
    }

    /// <summary>
    /// 取指定类型的第一条声明值(在主体的全部身份里找)
    /// </summary>
    /// <param name="principal">主体</param>
    /// <param name="claimType">声明类型</param>
    /// <returns>声明值, 不存在为 null</returns>
    public static string? FindClaimValue(this ClaimsPrincipal principal, string claimType)
    {
        return principal.FindClaim(claimType)?.Value;
    }

    /// <summary>
    /// 移除身份下所有指定类型的声明
    /// </summary>
    /// <param name="identity">身份</param>
    /// <param name="claimType">声明类型</param>
    public static void RemoveAll(this ClaimsIdentity identity, string claimType)
    {
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentNullException.ThrowIfNull(claimType);

        foreach (var claim in identity.FindAll(claimType).ToList())
        {
            identity.RemoveClaim(claim);
        }
    }

    /// <summary>
    /// 移除主体下所有指定类型的声明
    /// </summary>
    /// <param name="principal">主体</param>
    /// <param name="claimType">声明类型</param>
    public static void RemoveAll(this ClaimsPrincipal principal, string claimType)
    {
        ArgumentNullException.ThrowIfNull(principal);
        ArgumentNullException.ThrowIfNull(claimType);

        foreach (var identity in principal.Identities)
        {
            identity.RemoveAll(claimType);
        }
    }

    /// <summary>
    /// 解析用户Id
    /// </summary>
    /// <param name="value">声明值</param>
    /// <returns>用户Id, 缺失或非数字为 null</returns>
    private static long? ParseUserId(string? value)
    {
        return long.TryParse(value, out long userId) ? userId : null;
    }

    /// <summary>
    /// 解析租户 id
    /// </summary>
    /// <param name="value">声明值</param>
    /// <returns>租户 id, 缺失或非 Guid 为 null</returns>
    private static Guid? ParseTenantId(string? value)
    {
        return Guid.TryParse(value, out Guid tenantId) ? tenantId : null;
    }
}

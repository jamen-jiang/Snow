using System.Linq;
using System.Security.Claims;
using Rongia.Security;

namespace System.Security.Principal;

/// <summary>
/// <see cref="ClaimsPrincipal"/> 上的声明读取扩展(对齐 ABP <c>AbpClaimsIdentityExtensions</c> 的常用子集)
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// 取用户Id
    /// </summary>
    /// <param name="principal">主体</param>
    /// <returns>用户Id, 缺失或非数字为 null</returns>
    public static long? FindUserId(this ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        string? value = principal.FindClaimValue(UserClaimTypes.UserId);
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return long.TryParse(value, out long userId) ? userId : null;
    }

    /// <summary>
    /// 取指定类型的第一条声明值
    /// </summary>
    /// <param name="principal">主体</param>
    /// <param name="claimType">声明类型</param>
    /// <returns>声明值, 不存在为 null</returns>
    public static string? FindClaimValue(this ClaimsPrincipal principal, string claimType)
    {
        ArgumentNullException.ThrowIfNull(principal);

        return principal.Claims.FirstOrDefault(claim => claim.Type == claimType)?.Value;
    }
}

using System.Security.Claims;

namespace Snow.Security.Claims;

/// <summary>
/// 主体访问器的便捷重载: 直接用声明或身份临时替换当前主体
/// </summary>
public static class CurrentPrincipalAccessorExtensions
{
    /// <summary>
    /// 用单条声明临时替换当前主体
    /// </summary>
    /// <param name="currentPrincipalAccessor">主体访问器</param>
    /// <param name="claim">声明</param>
    /// <returns>释放后恢复原主体的句柄</returns>
    public static IDisposable Change(this ICurrentPrincipalAccessor currentPrincipalAccessor, Claim claim)
    {
        ArgumentNullException.ThrowIfNull(currentPrincipalAccessor);
        ArgumentNullException.ThrowIfNull(claim);

        return currentPrincipalAccessor.Change(new[] { claim });
    }

    /// <summary>
    /// 用一组声明临时替换当前主体
    /// </summary>
    /// <param name="currentPrincipalAccessor">主体访问器</param>
    /// <param name="claims">声明集合</param>
    /// <returns>释放后恢复原主体的句柄</returns>
    public static IDisposable Change(this ICurrentPrincipalAccessor currentPrincipalAccessor, IEnumerable<Claim> claims)
    {
        ArgumentNullException.ThrowIfNull(currentPrincipalAccessor);
        ArgumentNullException.ThrowIfNull(claims);

        return currentPrincipalAccessor.Change(new ClaimsIdentity(claims));
    }

    /// <summary>
    /// 用身份临时替换当前主体
    /// </summary>
    /// <param name="currentPrincipalAccessor">主体访问器</param>
    /// <param name="claimsIdentity">身份</param>
    /// <returns>释放后恢复原主体的句柄</returns>
    public static IDisposable Change(this ICurrentPrincipalAccessor currentPrincipalAccessor, ClaimsIdentity claimsIdentity)
    {
        ArgumentNullException.ThrowIfNull(currentPrincipalAccessor);
        ArgumentNullException.ThrowIfNull(claimsIdentity);

        return currentPrincipalAccessor.Change(new ClaimsPrincipal(claimsIdentity));
    }
}

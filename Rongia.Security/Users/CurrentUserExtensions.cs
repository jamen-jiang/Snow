namespace Rongia.Security.Users;

/// <summary>
/// <see cref="ICurrentUser"/> 的便捷扩展
/// </summary>
public static class CurrentUserExtensions
{
    /// <summary>
    /// 取指定类型的第一条声明值
    /// </summary>
    /// <param name="currentUser">当前用户</param>
    /// <param name="claimType">声明类型</param>
    /// <returns>声明值, 不存在为 null</returns>
    public static string? FindClaimValue(this ICurrentUser currentUser, string claimType)
    {
        ArgumentNullException.ThrowIfNull(currentUser);

        return currentUser.FindClaim(claimType)?.Value;
    }

    /// <summary>
    /// 取用户Id, 未认证时抛异常(已认证场景下使用)
    /// </summary>
    /// <param name="currentUser">当前用户</param>
    /// <returns>用户Id</returns>
    /// <exception cref="InvalidOperationException">当前用户未认证</exception>
    public static long GetId(this ICurrentUser currentUser)
    {
        ArgumentNullException.ThrowIfNull(currentUser);

        return currentUser.Id ?? throw new InvalidOperationException("当前用户未认证, 取不到用户Id。");
    }

    /// <summary>
    /// 取会话Id
    /// </summary>
    /// <param name="currentUser">当前用户</param>
    /// <returns>会话Id, 不存在为 null</returns>
    public static string? FindSessionId(this ICurrentUser currentUser)
    {
        ArgumentNullException.ThrowIfNull(currentUser);

        return currentUser.FindClaimValue(UserClaimTypes.SessionId);
    }
}

using System.Security.Claims;

namespace Snow.Authorization.Permissions;

/// <summary>
/// 单权限求值上下文
/// </summary>
public sealed class PermissionValueCheckContext
{
    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="permission">权限定义</param>
    /// <param name="principal">身份</param>
    public PermissionValueCheckContext(PermissionDefinition permission, ClaimsPrincipal? principal)
    {
        ArgumentNullException.ThrowIfNull(permission);
        Permission = permission;
        Principal = principal;
    }

    /// <summary>
    /// 权限定义
    /// </summary>
    public PermissionDefinition Permission { get; }

    /// <summary>
    /// 身份(可能为 null, 表示匿名)
    /// </summary>
    public ClaimsPrincipal? Principal { get; }
}

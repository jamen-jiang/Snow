using System.Security.Claims;

namespace Snow.Authorization.Permissions;

/// <summary>
/// 多权限求值上下文
/// </summary>
public sealed class PermissionValuesCheckContext
{
    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="permissions">权限定义集合</param>
    /// <param name="principal">身份</param>
    public PermissionValuesCheckContext(IReadOnlyList<PermissionDefinition> permissions, ClaimsPrincipal? principal)
    {
        ArgumentNullException.ThrowIfNull(permissions);
        Permissions = permissions;
        Principal = principal;
    }

    /// <summary>
    /// 权限定义集合
    /// </summary>
    public IReadOnlyList<PermissionDefinition> Permissions { get; }

    /// <summary>
    /// 身份(可能为 null, 表示匿名)
    /// </summary>
    public ClaimsPrincipal? Principal { get; }
}

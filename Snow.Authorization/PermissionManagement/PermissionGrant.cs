namespace Snow.PermissionManagement;

/// <summary>
/// 权限授予记录: (权限名, 提供器名, 提供器键) 三元组代表"某主体拥有某权限"
/// </summary>
public sealed class PermissionGrant
{
    /// <summary>
    /// 主键
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// 权限名
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 提供器名, 如 U(用户) / R(角色) / C(客户端)
    /// </summary>
    public string ProviderName { get; set; } = string.Empty;

    /// <summary>
    /// 提供器键, 如 用户Id / 角色名 / 客户端Id
    /// </summary>
    public string ProviderKey { get; set; } = string.Empty;
}

namespace Rongia.Authorization.Permissions;

/// <summary>
/// 权限分组: 权限树的根, 用于界面分组展示
/// </summary>
public sealed class PermissionGroupDefinition
{
    private readonly List<PermissionDefinition> _permissions = [];

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="name">分组名(唯一)</param>
    /// <param name="displayName">显示名称</param>
    internal PermissionGroupDefinition(string name, string? displayName = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
        DisplayName = displayName ?? name;
    }

    /// <summary>
    /// 分组名
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 显示名称
    /// </summary>
    public string DisplayName { get; set; }

    /// <summary>
    /// 分组下的根权限
    /// </summary>
    public IReadOnlyList<PermissionDefinition> Permissions => _permissions;

    /// <summary>
    /// 自定义属性
    /// </summary>
    public Dictionary<string, object?> Properties { get; } = [];

    /// <summary>
    /// 增加根权限
    /// </summary>
    /// <param name="name">权限全名</param>
    /// <param name="displayName">显示名称</param>
    /// <returns>权限定义</returns>
    public PermissionDefinition AddPermission(string name, string? displayName = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        PermissionDefinition permission = new(name, displayName);
        _permissions.Add(permission);
        return permission;
    }
}

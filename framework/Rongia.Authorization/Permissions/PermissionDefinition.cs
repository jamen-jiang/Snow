namespace Rongia.Authorization.Permissions;

/// <summary>
/// 权限定义: 权限树上的一個节点
/// </summary>
public sealed class PermissionDefinition
{
    private readonly List<PermissionDefinition> _children = [];

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="name">权限全名, 如 Rongia.Orders.Create</param>
    /// <param name="displayName">显示名称</param>
    internal PermissionDefinition(string name, string? displayName = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
        DisplayName = displayName ?? name;
    }

    /// <summary>
    /// 权限全名(唯一)
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 显示名称
    /// </summary>
    public string DisplayName { get; set; }

    /// <summary>
    /// 父权限; 有父权限时, 子权限授予前应先授予父权限
    /// </summary>
    public PermissionDefinition? Parent { get; private set; }

    /// <summary>
    /// 是否启用; 禁用的权限永远校验不通过
    /// </summary>
    public bool IsEnabled { get; private set; } = true;

    /// <summary>
    /// 允许对该权限求值的提供器名集合; 为空表示不限制
    /// </summary>
    public List<string> Providers { get; } = [];

    /// <summary>
    /// 子权限
    /// </summary>
    public IReadOnlyList<PermissionDefinition> Children => _children;

    /// <summary>
    /// 自定义属性
    /// </summary>
    public Dictionary<string, object?> Properties { get; } = [];

    /// <summary>
    /// 增加子权限
    /// </summary>
    /// <param name="name">子权限全名</param>
    /// <param name="displayName">显示名称</param>
    /// <returns>子权限定义</returns>
    public PermissionDefinition AddChild(string name, string? displayName = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        PermissionDefinition child = new(name, displayName) { Parent = this };
        _children.Add(child);
        return child;
    }

    /// <summary>
    /// 停用该权限
    /// </summary>
    /// <returns>自身, 便于链式调用</returns>
    public PermissionDefinition Disable()
    {
        IsEnabled = false;
        return this;
    }
}

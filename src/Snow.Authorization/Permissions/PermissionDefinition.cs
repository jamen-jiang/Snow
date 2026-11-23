namespace Snow.Authorization.Permissions;

/// <summary>
/// 权限分组: 权限树的根, 管理界面按分组归类
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
    /// 增加根权限
    /// </summary>
    /// <param name="name">权限全名, 建议点号分层(如 Snow.Orders.Create)</param>
    /// <param name="displayName">显示名称</param>
    /// <returns>权限定义, 可继续 AddChild / Disable</returns>
    public PermissionDefinition AddPermission(string name, string? displayName = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        PermissionDefinition permission = new(name, displayName);
        _permissions.Add(permission);
        return permission;
    }

    /// <summary>
    /// 把分组下的权限(含子权限)收进一张表, 重名直接抛
    /// </summary>
    /// <param name="target">目标表</param>
    internal void Collect(Dictionary<string, PermissionDefinition> target)
    {
        foreach (PermissionDefinition permission in _permissions)
        {
            permission.Collect(target);
        }
    }
}

/// <summary>
/// 权限定义: 权限树上的一个节点
/// <para>只有定义过的权限名才会被识破成策略; 名字写错拿不到策略, 请求直接 500。</para>
/// </summary>
public sealed class PermissionDefinition
{
    private readonly List<PermissionDefinition> _children = [];

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="name">权限全名</param>
    /// <param name="displayName">显示名称</param>
    internal PermissionDefinition(string name, string? displayName = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name;
        DisplayName = displayName ?? name;
    }

    /// <summary>
    /// 权限全名(全局唯一)
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 显示名称
    /// </summary>
    public string DisplayName { get; set; }

    /// <summary>
    /// 父权限; 仅用于展示层级, 不参与判定(子权限要单独授予)
    /// </summary>
    public PermissionDefinition? Parent { get; private set; }

    /// <summary>
    /// 是否启用; 禁用的权限永远校验不通过
    /// </summary>
    public bool IsEnabled { get; private set; } = true;

    /// <summary>
    /// 子权限
    /// </summary>
    public IReadOnlyList<PermissionDefinition> Children => _children;

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

    /// <summary>
    /// 把自己与子权限收进一张表, 重名直接抛
    /// </summary>
    /// <param name="target">目标表</param>
    internal void Collect(Dictionary<string, PermissionDefinition> target)
    {
        if (!target.TryAdd(Name, this))
        {
            throw new InvalidOperationException("权限名重复: " + Name + " —— 同一个权限名只能定义一次。");
        }

        foreach (PermissionDefinition child in _children)
        {
            child.Collect(target);
        }
    }
}

namespace Snow.Authorization.Permissions;

/// <summary>
/// 权限定义上下文: 定义提供器通过它登记分组与权限
/// </summary>
public sealed class PermissionDefinitionContext
{
    private readonly Dictionary<string, PermissionGroupDefinition> _groups = new(StringComparer.Ordinal);

    /// <summary>
    /// 增加分组
    /// </summary>
    /// <param name="name">分组名</param>
    /// <param name="displayName">显示名称</param>
    /// <returns>分组定义</returns>
    public PermissionGroupDefinition AddGroup(string name, string? displayName = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (_groups.ContainsKey(name))
        {
            throw new InvalidOperationException("已存在同名权限分组: " + name);
        }

        PermissionGroupDefinition group = new(name, displayName);
        _groups[name] = group;
        return group;
    }

    /// <summary>
    /// 取分组, 不存在抛异常
    /// </summary>
    /// <param name="name">分组名</param>
    /// <returns>分组定义</returns>
    public PermissionGroupDefinition GetGroup(string name)
        => GetGroupOrNull(name) ?? throw new KeyNotFoundException("未找到权限分组: " + name);

    /// <summary>
    /// 取分组, 不存在返回 null
    /// </summary>
    /// <param name="name">分组名</param>
    /// <returns>分组定义或 null</returns>
    public PermissionGroupDefinition? GetGroupOrNull(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return _groups.TryGetValue(name, out PermissionGroupDefinition? group) ? group : null;
    }

    /// <summary>
    /// 全部分组
    /// </summary>
    internal IEnumerable<PermissionGroupDefinition> Groups => _groups.Values;
}

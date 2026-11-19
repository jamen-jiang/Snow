namespace Rongia.Authorization.Permissions;

/// <summary>
/// 权限定义上下文默认实现
/// </summary>
public sealed class PermissionDefinitionContext : IPermissionDefinitionContext
{
    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="serviceProvider">服务提供器</param>
    public PermissionDefinitionContext(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ServiceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public IServiceProvider ServiceProvider { get; }

    /// <summary>
    /// 分组集合(按名称索引)
    /// </summary>
    public Dictionary<string, PermissionGroupDefinition> Groups { get; } = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public PermissionGroupDefinition AddGroup(string name, string? displayName = null)
    {
        if (Groups.ContainsKey(name))
        {
            throw new InvalidOperationException($"已存在同名权限分组: {name}");
        }

        PermissionGroupDefinition group = new(name, displayName);
        Groups[name] = group;
        return group;
    }

    /// <inheritdoc />
    public PermissionGroupDefinition GetGroup(string name)
        => GetGroupOrNull(name) ?? throw new InvalidOperationException($"未找到权限分组: {name}");

    /// <inheritdoc />
    public PermissionGroupDefinition? GetGroupOrNull(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return Groups.TryGetValue(name, out PermissionGroupDefinition? group) ? group : null;
    }
}

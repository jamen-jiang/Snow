namespace Snow.MultiTenancy;

/// <summary>
/// 租户配置(对齐 ABP 的 <c>TenantConfiguration</c>)
/// <para>租户存储返回它; 连接串用字典承载, 键名不区分大小写, 空字典表示沿用宿主连接串。</para>
/// </summary>
public sealed class TenantConfiguration
{
    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="id">租户 id</param>
    /// <param name="name">租户名称</param>
    public TenantConfiguration(Guid id, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Id = id;
        Name = name;
    }

    /// <summary>
    /// 租户 id
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// 租户名称, 也是租户的唯一标识
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 租户是否可用, 停用的租户不应被解析出来
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// 租户级连接串, 键名不区分大小写; 空表示沿用宿主连接串
    /// </summary>
    public Dictionary<string, string?> ConnectionStrings { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 默认连接串
    /// </summary>
    public string? DefaultConnectionString
    {
        get => ConnectionStrings.TryGetValue("Default", out string? value) ? value : null;
        set => ConnectionStrings["Default"] = value;
    }
}

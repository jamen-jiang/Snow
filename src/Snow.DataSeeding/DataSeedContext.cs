namespace Snow.DataSeeding;

/// <summary>
/// 播种上下文: 传给数据出资者的环境信息
/// </summary>
public sealed class DataSeedContext
{
    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="tenantId">租户 id, null 表示宿主</param>
    public DataSeedContext(Guid? tenantId = null) => TenantId = tenantId;

    /// <summary>
    /// 租户 id, null 表示宿主
    /// </summary>
    public Guid? TenantId { get; set; }

    /// <summary>
    /// 附加属性, 出资者可自由读写
    /// </summary>
    public Dictionary<string, object?> Properties { get; } = new(StringComparer.Ordinal);

    /// <summary>
    /// 按名读写附加属性
    /// </summary>
    /// <param name="key">键</param>
    public object? this[string key]
    {
        get => Properties.TryGetValue(key, out object? value) ? value : null;
        set => Properties[key] = value;
    }
}

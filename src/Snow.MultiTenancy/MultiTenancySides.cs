namespace Snow.MultiTenancy;

/// <summary>
/// 多租户侧: 标识一段代码运行在宿主侧还是租户侧(对齐 ABP 的 <c>MultiTenancySides</c>)
/// </summary>
[Flags]
public enum MultiTenancySides
{
    /// <summary>
    /// 未指定
    /// </summary>
    None = 0,

    /// <summary>
    /// 宿主侧
    /// </summary>
    Host = 1,

    /// <summary>
    /// 租户侧
    /// </summary>
    Tenant = 2,

    /// <summary>
    /// 两侧都适用
    /// </summary>
    All = Host | Tenant
}

using System;

namespace Snow.MultiTenancy;

/// <summary>
/// 基础租户信息
/// </summary>
public class BasicTenantInfo
{
    /// <summary>
    /// 租户Id, 空表示宿主
    /// </summary>
    public Guid? TenantId { get; }

    /// <summary>
    /// 租户名称
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="tenantId">租户Id</param>
    /// <param name="name">租户名称</param>
    public BasicTenantInfo(Guid? tenantId, string? name = null)
    {
        TenantId = tenantId;
        Name = name;
    }
}

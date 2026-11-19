using System;

namespace Rongia.MultiTenancy;

/// <summary>
/// 当前租户
/// </summary>
public interface ICurrentTenant
{
    /// <summary>
    /// 是否已解析出确定的租户(含宿主)
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// 租户Id, 空表示宿主
    /// </summary>
    Guid? Id { get; }

    /// <summary>
    /// 租户名称
    /// </summary>
    string? Name { get; }

    /// <summary>
    /// 临时切换到指定租户, 释放后恢复原值
    /// </summary>
    /// <param name="id">租户Id</param>
    /// <param name="name">租户名称</param>
    /// <returns>用于恢复的作用域</returns>
    IDisposable Change(Guid? id, string? name = null);
}

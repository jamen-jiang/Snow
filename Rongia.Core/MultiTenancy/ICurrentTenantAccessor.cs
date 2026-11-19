namespace Rongia.MultiTenancy;

/// <summary>
/// 当前租户访问器: 由宿主在请求进入时写入, 释放时还原
/// </summary>
public interface ICurrentTenantAccessor
{
    /// <summary>
    /// 当前租户信息
    /// </summary>
    BasicTenantInfo? Current { get; set; }
}

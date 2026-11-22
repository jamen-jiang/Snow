namespace Snow.MultiTenancy;

/// <summary>
/// 多租户开关(对齐 ABP 的 <c>IMultiTenancyConfig</c>)
/// <para>关闭时当前租户恒为宿主, <see cref="ICurrentTenant.Change"/> 不生效。</para>
/// </summary>
public interface IMultiTenancyConfig
{
    /// <summary>
    /// 是否启用多租户
    /// </summary>
    bool IsEnabled { get; }
}

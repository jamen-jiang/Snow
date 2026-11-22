namespace Snow.MultiTenancy;

/// <summary>
/// 当前租户信息的存取口子(对齐 ABP 的 <c>ICurrentTenantAccessor</c>)
/// <para>把状态容器与读取逻辑分开: 容器可被替换, <see cref="ICurrentTenant"/> 始终从容器读。</para>
/// </summary>
public interface ICurrentTenantAccessor
{
    /// <summary>
    /// 当前租户信息, null 表示宿主或尚未设置
    /// </summary>
    TenantInfo? Current { get; set; }
}

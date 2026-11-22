namespace Snow.AspNetCore.MultiTenancy;

/// <summary>
/// Web 层多租户选项(对齐 ABP 的 <c>AbpAspNetCoreMultiTenancyOptions</c>)
/// <para>只放请求侧的约定: 四个出资者都读同一个键名, 默认 <c>__tenant</c>, 与 ABP 一致。</para>
/// </summary>
public sealed class SnowAspNetCoreMultiTenancyOptions
{
    /// <summary>
    /// 查询串 / 路由 / 请求头 / Cookie 里表示租户的键名
    /// </summary>
    public string TenantKey { get; set; } = "__tenant";
}

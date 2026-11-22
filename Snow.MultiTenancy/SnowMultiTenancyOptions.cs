namespace Snow.MultiTenancy;

/// <summary>
/// 多租户选项(对齐 ABP 的 <c>AbpMultiTenancyOptions</c>)
/// <para>默认关闭, 与 ABP 一致: 不做多租户的应用不需要关心这套上下文。</para>
/// </summary>
public sealed class SnowMultiTenancyOptions : IMultiTenancyConfig
{
    /// <summary>
    /// 是否启用多租户
    /// </summary>
    public bool IsEnabled { get; set; }
}

namespace Snow.Settings;

/// <summary>
/// 设置选项(对齐 ABP 的 <c>AbpSettingOptions</c>)
/// <para>这里是值提供器的登记顺序: 列表越靠后优先级越高(读取时整个列表反过来遍历), 与 ABP 一致。
/// 默认顺序 默认值 → 配置 → 全局存储, 即 全局存储 &gt; 配置 &gt; 默认值。</para>
/// </summary>
public sealed class SnowSettingOptions
{
    /// <summary>
    /// 值提供器类型清单, 靠后的优先级更高
    /// </summary>
    public List<Type> ValueProviders { get; } =
    [
        typeof(DefaultValueSettingValueProvider),
        typeof(ConfigurationSettingValueProvider),
        typeof(GlobalSettingValueProvider),
    ];
}

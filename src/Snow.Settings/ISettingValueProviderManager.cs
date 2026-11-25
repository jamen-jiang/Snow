namespace Snow.Settings;

/// <summary>
/// 已实例化的取值来源清单(对应 ABP 的 ISettingValueProviderManager)
/// </summary>
public interface ISettingValueProviderManager
{
    /// <summary>
    /// 全部来源, 顺序与登记顺序一致
    /// </summary>
    IReadOnlyList<ISettingValueProvider> Providers { get; }
}

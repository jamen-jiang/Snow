namespace Snow.Settings;

/// <summary>
/// 值提供器管理器(对齐 ABP 的 <c>ISettingValueProviderManager</c>): 按 <see cref="SnowSettingOptions"/> 的登记顺序实例化提供器
/// </summary>
public interface ISettingValueProviderManager
{
    /// <summary>
    /// 全部值提供器, 顺序即登记顺序(靠后的优先级更高)
    /// </summary>
    IReadOnlyList<ISettingValueProvider> Providers { get; }
}

namespace Snow.Settings;

/// <summary>
/// 配置项取值来源(对应 ABP 的 ISettingValueProvider): 一个来源负责一类存储, 例如配置节、数据库、租户设置
/// <para><see cref="SettingProvider"/> 按登记顺序倒着问, 第一个给出非 null 值的来源获胜。</para>
/// </summary>
public interface ISettingValueProvider
{
    /// <summary>
    /// 来源名; <see cref="SettingDefinition.Providers"/> 里写的就是它
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 取一个配置项的值
    /// </summary>
    /// <param name="setting">配置项定义</param>
    /// <returns>没有该来源的值时返回 null</returns>
    Task<string?> GetOrNullAsync(SettingDefinition setting);
}

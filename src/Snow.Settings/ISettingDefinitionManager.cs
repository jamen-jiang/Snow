namespace Snow.Settings;

/// <summary>
/// 汇总所有 <see cref="SettingDefinitionProvider"/> 之后的定义表(对应 ABP 的 ISettingDefinitionManager)
/// </summary>
public interface ISettingDefinitionManager
{
    /// <summary>
    /// 按名字取定义, 没有则抛异常
    /// </summary>
    /// <param name="name">配置项名</param>
    /// <returns>定义</returns>
    SettingDefinition Get(string name);

    /// <summary>
    /// 按名字取定义
    /// </summary>
    /// <param name="name">配置项名</param>
    /// <returns>没有时返回 null</returns>
    SettingDefinition? GetOrNull(string name);

    /// <summary>
    /// 全部定义
    /// </summary>
    /// <returns>定义列表</returns>
    IReadOnlyList<SettingDefinition> GetAll();
}

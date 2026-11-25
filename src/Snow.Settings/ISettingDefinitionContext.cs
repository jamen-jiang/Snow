namespace Snow.Settings;

/// <summary>
/// <see cref="SettingDefinitionProvider"/> 的登记上下文(对应 ABP 的 ISettingDefinitionContext)
/// </summary>
public interface ISettingDefinitionContext
{
    /// <summary>
    /// 按名字取已登记的定义
    /// </summary>
    /// <param name="name">配置项名</param>
    /// <returns>没有时返回 null</returns>
    SettingDefinition? GetOrNull(string name);

    /// <summary>
    /// 登记若干定义
    /// </summary>
    /// <param name="definitions">定义</param>
    void Add(params SettingDefinition[] definitions);
}

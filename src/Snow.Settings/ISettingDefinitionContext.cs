namespace Snow.Settings;

/// <summary>
/// 设置定义上下文: 各提供器往这里贡献定义
/// </summary>
public interface ISettingDefinitionContext
{
    /// <summary>
    /// 新增一个定义; 同名已存在时覆盖
    /// </summary>
    /// <param name="definition">设置定义</param>
    void Add(SettingDefinition definition);

    /// <summary>
    /// 按名取已添加的定义
    /// </summary>
    /// <param name="name">设置名</param>
    /// <returns>定义, 不存在为 null</returns>
    SettingDefinition? GetOrNull(string name);
}

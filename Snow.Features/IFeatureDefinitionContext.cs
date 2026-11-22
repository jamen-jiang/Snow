namespace Snow.Features;

/// <summary>
/// 功能定义上下文: 各提供器往这里贡献定义
/// </summary>
public interface IFeatureDefinitionContext
{
    /// <summary>
    /// 新增一个定义; 同名已存在时覆盖
    /// </summary>
    /// <param name="definition">功能定义</param>
    void Add(FeatureDefinition definition);

    /// <summary>
    /// 按名取已添加的定义
    /// </summary>
    /// <param name="name">功能名</param>
    /// <returns>定义, 不存在为 null</returns>
    FeatureDefinition? GetOrNull(string name);
}

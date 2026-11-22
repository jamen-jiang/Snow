namespace Snow.Features;

/// <summary>
/// 功能定义: 一个功能开关的元数据与默认值
/// </summary>
public sealed class FeatureDefinition
{
    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="name">功能名, 全局唯一</param>
    /// <param name="defaultValue">默认值, 布尔开关写 "true" / "false"</param>
    /// <param name="displayName">显示名</param>
    /// <param name="description">说明</param>
    public FeatureDefinition(string name, string? defaultValue = null, string? displayName = null, string? description = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name;
        DefaultValue = defaultValue;
        DisplayName = displayName;
        Description = description;
    }

    /// <summary>
    /// 功能名
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 默认值
    /// </summary>
    public string? DefaultValue { get; set; }

    /// <summary>
    /// 显示名
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// 说明
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 值类型, 默认按字符串处理
    /// </summary>
    public Type ValueType { get; set; } = typeof(string);

    /// <summary>
    /// 附加属性, 供 UI 或扩展使用
    /// </summary>
    public Dictionary<string, object?> Properties { get; } = new(StringComparer.Ordinal);
}

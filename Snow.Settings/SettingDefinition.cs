namespace Snow.Settings;

/// <summary>
/// 设置定义: 一个设置的元数据与默认值
/// </summary>
public sealed class SettingDefinition
{
    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="name">设置名, 全局唯一, 建议用点分层级(如 <c>MyApp.Mail.Host</c>)</param>
    /// <param name="defaultValue">默认值</param>
    /// <param name="displayName">显示名</param>
    /// <param name="description">说明</param>
    /// <param name="isEncrypted">值是否加密保存(提示存储层加密, 明文读取不变)</param>
    public SettingDefinition(string name, string? defaultValue = null, string? displayName = null, string? description = null, bool isEncrypted = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name;
        DefaultValue = defaultValue;
        DisplayName = displayName;
        Description = description;
        IsEncrypted = isEncrypted;
    }

    /// <summary>
    /// 设置名
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
    /// 值是否加密保存
    /// </summary>
    public bool IsEncrypted { get; set; }

    /// <summary>
    /// 所属分组, 便于管理界面归类
    /// </summary>
    public string? Group { get; set; }

    /// <summary>
    /// 附加属性, 供 UI 或扩展使用
    /// </summary>
    public Dictionary<string, object?> Properties { get; } = new(StringComparer.Ordinal);
}

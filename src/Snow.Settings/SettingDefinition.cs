namespace Snow.Settings;

/// <summary>
/// 配置项定义: 名称、默认值与是否加密
/// </summary>
public sealed class SettingDefinition
{
    /// <summary>配置项名称, 在 Settings 配置节内唯一</summary>
    public string Name { get; }

    /// <summary>未配置时使用的默认值</summary>
    public string? DefaultValue { get; }

    /// <summary>读取后是否需要解密</summary>
    public bool IsEncrypted { get; }

    /// <summary>
    /// 创建配置项定义
    /// </summary>
    /// <param name="name">配置项名称</param>
    /// <param name="defaultValue">默认值</param>
    /// <param name="isEncrypted">是否加密</param>
    public SettingDefinition(string name, string? defaultValue = null, bool isEncrypted = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name;
        DefaultValue = defaultValue;
        IsEncrypted = isEncrypted;
    }
}

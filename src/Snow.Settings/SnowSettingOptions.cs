using Snow.Collections;

namespace Snow.Settings;

/// <summary>
/// 配置项选项: 注册定义提供器并控制解密失败行为
/// </summary>
public class SnowSettingOptions
{
    /// <summary>配置项定义提供器类型</summary>
    public ITypeList<SettingDefinitionProvider> DefinitionProviders { get; }

    /// <summary>从最终定义表中删除的配置项名称</summary>
    public HashSet<string> DeletedSettings { get; }

    /// <summary>解密失败时是否返回原文(而不是空串), 默认 true</summary>
    public bool ReturnOriginalValueIfDecryptFailed { get; set; }

    public SnowSettingOptions()
    {
        DefinitionProviders = new TypeList<SettingDefinitionProvider>();
        DeletedSettings = new HashSet<string>();
        ReturnOriginalValueIfDecryptFailed = true;
    }
}

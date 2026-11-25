using Snow.Collections;

namespace Snow.Settings;

/// <summary>
/// 配置项系统的选项(对应 ABP 的 AbpSettingOptions)
/// </summary>
public class SnowSettingOptions
{
    /// <summary>
    /// 定义提供器
    /// </summary>
    public ITypeList<SettingDefinitionProvider> DefinitionProviders { get; }

    /// <summary>
    /// 取值来源
    /// </summary>
    public ITypeList<ISettingValueProvider> ValueProviders { get; }

    /// <summary>
    /// 从定义表里删掉的名字
    /// </summary>
    public HashSet<string> DeletedSettings { get; }

    /// <summary>
    /// 解密失败时是否返回原文(而不是空串), 默认 true
    /// </summary>
    public bool ReturnOriginalValueIfDecryptFailed { get; set; }

    /// <summary>
    /// 构造器
    /// </summary>
    public SnowSettingOptions()
    {
        DefinitionProviders = new TypeList<SettingDefinitionProvider>();
        ValueProviders = new TypeList<ISettingValueProvider>();
        DeletedSettings = new HashSet<string>();
        ReturnOriginalValueIfDecryptFailed = true;
    }
}

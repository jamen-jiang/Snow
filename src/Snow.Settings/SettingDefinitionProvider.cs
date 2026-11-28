namespace Snow.Settings;

/// <summary>
/// 配置项定义提供器基类: 模块声明配置项名称、默认值与是否加密
/// </summary>
public abstract class SettingDefinitionProvider
{
    /// <summary>声明配置项</summary>
    public abstract void Define(SettingDefinitionContext context);
}

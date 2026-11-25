namespace Snow.Settings;

/// <summary>
/// 一次批量取值的结果: 配置项名 + 值(对应 ABP 的 SettingValue)
/// </summary>
public class SettingValue
{
    /// <summary>
    /// 配置项名
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 值; 所有提供器都给不出时为 null
    /// </summary>
    public string? Value { get; }

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="name">配置项名</param>
    /// <param name="value">值</param>
    public SettingValue(string name, string? value)
    {
        Name = name;
        Value = value;
    }
}

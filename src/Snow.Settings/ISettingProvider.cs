namespace Snow.Settings;

/// <summary>
/// 配置项读取入口: 从 IConfiguration 的 Settings 节读取值, 应用默认值并按需解密
/// </summary>
public interface ISettingProvider
{
    /// <summary>
    /// 读取配置项; 配置项未定义时返回 null
    /// </summary>
    /// <param name="name">配置项名称</param>
    /// <returns>配置值或 null</returns>
    Task<string?> GetOrNullAsync(string name);
}

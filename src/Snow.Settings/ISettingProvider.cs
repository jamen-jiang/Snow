namespace Snow.Settings;

/// <summary>
/// 读配置项的门面(对应 ABP 的 ISettingProvider): 按定义走所有取值来源, 加密的直接给明文
/// </summary>
public interface ISettingProvider
{
    /// <summary>
    /// 取一个配置项的值
    /// </summary>
    /// <param name="name">配置项名</param>
    /// <returns>所有来源都没有、也没有默认值时返回 null</returns>
    Task<string?> GetOrNullAsync(string name);

    /// <summary>
    /// 取一个配置项的值, 取不到抛异常
    /// </summary>
    /// <param name="name">配置项名</param>
    /// <returns>值</returns>
    Task<string> GetAsync(string name);

    /// <summary>
    /// 批量取值
    /// </summary>
    /// <param name="names">配置项名</param>
    /// <returns>每个名字一项, 顺序与入参一致</returns>
    Task<List<SettingValue>> GetAllAsync(string[] names);

    /// <summary>
    /// 取全部已定义配置项的值
    /// </summary>
    /// <returns>每个定义一项</returns>
    Task<List<SettingValue>> GetAllAsync();
}

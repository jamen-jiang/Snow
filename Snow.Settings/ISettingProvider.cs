namespace Snow.Settings;

/// <summary>
/// 设置读取入口: 按提供器链解析出最终值
/// </summary>
public interface ISettingProvider
{
    /// <summary>
    /// 取设置值
    /// </summary>
    /// <param name="name">设置名</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>设置值, 未定义或无值为 null</returns>
    Task<string?> GetOrNullAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// 取设置值并转成目标类型
    /// </summary>
    /// <param name="name">设置名</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <typeparam name="T">目标类型</typeparam>
    /// <returns>转换后的值, 无值为默认值</returns>
    Task<T?> GetAsync<T>(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// 取全部已定义设置的值
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>键值对集合</returns>
    Task<IReadOnlyList<SettingValue>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 取指定设置的值
    /// </summary>
    /// <param name="names">设置名集合</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>键值对集合</returns>
    Task<IReadOnlyList<SettingValue>> GetAllAsync(string[] names, CancellationToken cancellationToken = default);
}

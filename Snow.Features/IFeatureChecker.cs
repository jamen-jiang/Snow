namespace Snow.Features;

/// <summary>
/// 功能开关检查入口
/// </summary>
public interface IFeatureChecker
{
    /// <summary>
    /// 功能是否开启
    /// </summary>
    /// <param name="name">功能名</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>开启为 true</returns>
    Task<bool> IsEnabledAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// 取功能值
    /// </summary>
    /// <param name="name">功能名</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>功能值, 未定义为 null</returns>
    Task<string?> GetOrNullAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// 取功能值并转成目标类型
    /// </summary>
    /// <param name="name">功能名</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <typeparam name="T">目标类型</typeparam>
    /// <returns>转换后的值, 无值为默认值</returns>
    Task<T?> GetValueAsync<T>(string name, CancellationToken cancellationToken = default);
}

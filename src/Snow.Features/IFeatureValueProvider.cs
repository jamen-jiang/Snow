namespace Snow.Features;

/// <summary>
/// 功能值提供器: 按优先级从某个来源取功能值
/// <para>取到的第一个非 null 值生效; 提供器顺序由 <see cref="Name"/> 决定, 数值越小越优先。</para>
/// </summary>
public interface IFeatureValueProvider
{
    /// <summary>
    /// 提供器名, 同时决定优先级(越小越优先)
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 取功能值
    /// </summary>
    /// <param name="definition">功能定义</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>功能值, 本提供器无值时为 null</returns>
    Task<string?> GetOrNullAsync(FeatureDefinition definition, CancellationToken cancellationToken = default);
}

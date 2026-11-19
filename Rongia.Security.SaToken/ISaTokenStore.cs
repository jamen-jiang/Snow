namespace Rongia.Security.SaToken;

/// <summary>
/// 登录态存储: 令牌到登录态的持久化, 外加账号封禁标记
/// </summary>
public interface ISaTokenStore
{
    /// <summary>
    /// 按令牌查登录态
    /// </summary>
    Task<SaSession?> FindAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// 保存(新建或刷新)登录态
    /// </summary>
    Task SaveAsync(SaSession session, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除单个登录态(登出)
    /// </summary>
    Task DeleteAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// 查某账号的全部登录态
    /// </summary>
    Task<IReadOnlyList<SaSession>> FindByLoginIdAsync(string loginId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除某账号的全部登录态(踢人/单端登录)
    /// </summary>
    Task DeleteByLoginIdAsync(string loginId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 查账号封禁截止时间
    /// </summary>
    Task<DateTimeOffset?> FindDisableAsync(string loginId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 设置账号封禁截止时间, null 表示解除
    /// </summary>
    Task SetDisableAsync(string loginId, DateTimeOffset? until, CancellationToken cancellationToken = default);
}

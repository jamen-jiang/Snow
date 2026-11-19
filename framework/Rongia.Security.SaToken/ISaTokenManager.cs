namespace Rongia.Security.SaToken;

/// <summary>
/// 登录态管理器: 对应 Sa-Token 的 StpUtil(login / logout / kickout / disable / renewTimeout)
/// </summary>
public interface ISaTokenManager
{
    /// <summary>
    /// 登录: 创建登录态并返回令牌(账号被封禁时抛 <see cref="SaDisableException"/>)
    /// </summary>
    /// <param name="userId">用户Id</param>
    /// <param name="userName">登录账号</param>
    /// <param name="name">用户名称</param>
    /// <param name="claims">附加声明(权限、角色、手机号、菜单等)</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>令牌</returns>
    Task<string> LoginAsync(
        long userId,
        string userName,
        string name,
        IEnumerable<KeyValuePair<string, string>>? claims = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 登录: 只给用户Id, 账号与名称默认取用户Id(附加声明里若带账号/名称则以它为准)
    /// </summary>
    /// <param name="userId">用户Id</param>
    /// <param name="claims">附加声明</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>令牌</returns>
    Task<string> LoginAsync(
        long userId,
        IEnumerable<KeyValuePair<string, string>>? claims = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 登出: 删除令牌对应的登录态
    /// </summary>
    Task LogoutAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// 是否已登录
    /// </summary>
    Task<bool> IsLoginAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// 取登录Id, 未登录或已过期返回 null
    /// </summary>
    Task<long?> GetLoginIdAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// 校验并续期: 令牌有效则刷新最后活跃时间, 无效返回 null
    /// </summary>
    Task<SaSession?> GetSessionAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// 取令牌信息, 无效返回 null
    /// </summary>
    Task<SaTokenInfo?> GetTokenInfoAsync(
        string token,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 踢人: 删除该账号全部登录态
    /// </summary>
    /// <returns>被踢下线的数量</returns>
    Task<int> KickoutAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 查在线数量
    /// </summary>
    Task<int> GetOnlineCountAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 封禁账号: 立即踢掉全部登录态, 之后登录抛 <see cref="SaDisableException"/>
    /// </summary>
    /// <param name="userId">用户Id</param>
    /// <param name="duration">封禁时长, null 表示永久</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>被踢下线的数量</returns>
    Task<int> DisableAsync(
        long userId,
        TimeSpan? duration = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 解除封禁
    /// </summary>
    Task EnableAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 是否处于封禁中
    /// </summary>
    Task<bool> IsDisabledAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 剩余封禁时长: 未封禁 null, 永久 <see cref="TimeSpan.MaxValue"/>
    /// </summary>
    Task<TimeSpan?> GetDisableRemainingAsync(
        long userId,
        CancellationToken cancellationToken = default
    );
}

using System.Security.Claims;

namespace Snow.Authentication;

/// <summary>
/// 令牌服务: 签发、校验、登出、踢人、封禁、在线设备
/// <para>有效性模型是白名单 + 滑动续期: 签名通过、且服务端<b>仍存在未过期的会话</b>才算登录。
/// 登出、踢人、封禁都是删会话, 令牌立刻失效。</para>
/// <para>令牌自带账号与时间声明, 校验时不需要靠会话还原用户; 会话只存 <c>jti</c> → 账号与时间, 用来让令牌提前失效。
/// 令牌里的 <c>exp</c> 只是签发时的快照, 真正判过期的是会话的到期时间: 每次访问都把会话往后推一个窗口,
/// 所以持续使用的登录不会掉线, 停用超过一个窗口才需要重新登录。</para>
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// 登录: 签发令牌(账号被封禁时抛 <see cref="TokenDisabledException"/>)
    /// </summary>
    /// <param name="userId">用户Id</param>
    /// <param name="userName">登录账号</param>
    /// <param name="name">用户名称</param>
    /// <param name="claims">附加声明(权限、角色、手机号、菜单等), 会写进令牌</param>
    /// <param name="clientInfo">登录来源(IP/设备), 写进会话供设备列表展示; 传 null 时自动取当前请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>令牌</returns>
    Task<string> LoginAsync(
        long userId,
        string userName,
        string name,
        IEnumerable<KeyValuePair<string, string>>? claims = null,
        TokenClientInfo? clientInfo = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 登录: 只给用户Id, 账号与名称默认取用户Id(附加声明里若带账号/名称则以它为准)
    /// </summary>
    /// <param name="userId">用户 ID</param>
    /// <param name="claims">附加声明, 可为 null</param>
    /// <param name="clientInfo">客户端信息, 可为 null</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task<string> LoginAsync(
        long userId,
        IEnumerable<KeyValuePair<string, string>>? claims = null,
        TokenClientInfo? clientInfo = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 校验令牌: 签名、签发者、受众都通过且会话仍在有效期内, 返回主体; 否则 null
    /// <para>每次请求一次缓存读, 读的是会话键; 校验通过就写回续期(顺带刷新缓存键 TTL), 所以每次访问都有一次写。</para>
    /// </summary>
    /// <param name="token">令牌</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task<ClaimsPrincipal?> ValidateAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// 登出: 删掉当前令牌的会话。重复调用无副作用
    /// </summary>
    /// <param name="token">令牌</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task LogoutAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// 按令牌唯一标识下线(强制下线某台设备), 不需要令牌原文
    /// </summary>
    /// <param name="jti">令牌唯一标识</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>本次是否真的下线了一个此前有效的会话</returns>
    Task<bool> RevokeAsync(string jti, CancellationToken cancellationToken = default);

    /// <summary>
    /// 是否已登录(令牌有效且会话存在)
    /// </summary>
    /// <param name="token">令牌</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task<bool> IsLoginAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// 取登录Id, 未登录或已过期返回 null
    /// </summary>
    /// <param name="token">令牌</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task<long?> GetLoginIdAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// 取会话, 未登录返回 null
    /// </summary>
    /// <param name="token">令牌</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task<TokenSession?> GetSessionAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// 取令牌信息, 无效返回 null
    /// </summary>
    /// <param name="token">令牌</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task<TokenInfo?> GetTokenInfoAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// 取某账号的全部会话(设备列表: jti、登录时间、当前到期时间、IP、设备)
    /// </summary>
    /// <param name="userId">用户 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task<IReadOnlyList<TokenSession>> GetSessionsAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 踢人: 删掉该账号全部会话, 现有令牌立即失效
    /// <para>改密码、改角色、被管理员禁用等场景都应调用它, 否则旧令牌会靠滑动续期一直有效下去。</para>
    /// </summary>
    /// <param name="userId">用户 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>被踢下线的会话数量</returns>
    Task<int> KickoutAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 查在线数量
    /// </summary>
    /// <param name="userId">用户 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task<int> GetOnlineCountAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 封禁账号: 写封禁记录并删掉现有会话, 之后登录抛 <see cref="TokenDisabledException"/>
    /// </summary>
    /// <param name="userId">用户Id</param>
    /// <param name="duration">封禁时长, null 表示永久</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>被踢下线的会话数量</returns>
    Task<int> DisableAsync(long userId, TimeSpan? duration = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 解除封禁(已删掉的会话不会因此复活, 需要重新登录)
    /// </summary>
    /// <param name="userId">用户 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task EnableAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 是否处于封禁中
    /// </summary>
    /// <param name="userId">用户 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task<bool> IsDisabledAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 剩余封禁时长: 未封禁 null, 永久 <see cref="TimeSpan.MaxValue"/>
    /// </summary>
    /// <param name="userId">用户 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task<TimeSpan?> GetDisableRemainingAsync(long userId, CancellationToken cancellationToken = default);
}

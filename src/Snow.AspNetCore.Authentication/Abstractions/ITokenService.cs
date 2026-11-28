using System.Security.Claims;

namespace Snow.AspNetCore.Authentication;

/// <summary>
/// 令牌服务: 签发、校验、登出、踢人、封禁、在线设备
/// <para>有效性模型是白名单 + 滑动续期: 签名通过、且服务端仍存在未过期的会话才算登录;
/// 登出、踢人、封禁都是删会话, 令牌立刻失效。会话只存 <c>jti</c> → 账号与时间, 校验时不需要靠会话还原用户。</para>
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// 登录: 签发令牌(账号被封禁时抛 <see cref="TokenDisabledException"/>); claims 会写进令牌
    /// </summary>
    Task<string> LoginAsync(
        long userId,
        string userName,
        string name,
        IEnumerable<KeyValuePair<string, string>>? claims = null,
        TokenClientInfo? clientInfo = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 校验令牌: 签名、签发者、受众都通过且会话仍在有效期内, 返回主体; 否则 null
    /// </summary>
    Task<ClaimsPrincipal?> ValidateAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// 登出: 删掉当前令牌的会话, 重复调用无副作用
    /// </summary>
    Task LogoutAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// 按令牌唯一标识下线指定设备, 不需要令牌原文; 返回本次是否真的下线了一个此前有效的会话
    /// </summary>
    Task<bool> RevokeAsync(string jti, CancellationToken cancellationToken = default);

    /// <summary>
    /// 取当前令牌的会话, 未登录返回 null
    /// </summary>
    Task<TokenSession?> GetSessionAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// 取某账号的全部会话(设备列表: jti、登录时间、当前到期时间、IP、设备)
    /// </summary>
    Task<IReadOnlyList<TokenSession>> GetSessionsAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 查在线数量
    /// </summary>
    Task<int> GetOnlineCountAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 踢人: 删掉该账号全部会话, 现有令牌立即失效(改密码、改角色、被管理员禁用等场景都该调用)
    /// </summary>
    Task<int> KickoutAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 封禁账号: 写封禁记录并删掉现有会话, 之后登录抛 <see cref="TokenDisabledException"/>
    /// </summary>
    Task<int> DisableAsync(long userId, TimeSpan? duration = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 解除封禁(已删掉的会话不会因此复活, 需要重新登录)
    /// </summary>
    Task EnableAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 是否处于封禁中
    /// </summary>
    Task<bool> IsDisabledAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 剩余封禁时长: 未封禁 null, 永久 <see cref="TimeSpan.MaxValue"/>
    /// </summary>
    Task<TimeSpan?> GetDisableRemainingAsync(long userId, CancellationToken cancellationToken = default);
}

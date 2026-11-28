namespace Snow.AspNetCore.Authentication;

/// <summary>
/// 会话存储: 在线会话登记与账号封禁
/// <para>有效性模型是白名单 + 滑动续期: 签名通过、且服务端<b>仍存在</b>未过期的会话才算登录。
/// 每次访问都把会话的到期时间往后推, 所以持续使用的令牌不会因为签发太久而掉线, 停用超过一个窗口才失效。</para>
/// <para>登出、踢人、封禁都是删会话, 令牌立刻失效。</para>
/// </summary>
public interface ITokenSessionStore
{
    /// <summary>
    /// 登记会话(新建), 同时把它写进账号索引
    /// </summary>
    /// <param name="session">令牌会话</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task SaveAsync(TokenSession session, CancellationToken cancellationToken = default);

    /// <summary>
    /// 刷新会话的到期时间(滑动续期)
    /// <para>会话键与账号索引都重写一次: 索引的寿命也跟着续, 否则长期在线的账号会让索引先过期, 踢人时漏设备。</para>
    /// </summary>
    /// <param name="session">令牌会话</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task TouchAsync(TokenSession session, CancellationToken cancellationToken = default);

    /// <summary>
    /// 按令牌唯一标识查会话, 不存在表示该令牌已失效
    /// </summary>
    /// <param name="jti">会话 jti</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task<TokenSession?> FindAsync(string jti, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除单个会话(登出/下线指定设备)
    /// </summary>
    /// <param name="jti">会话 jti</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>被删除的会话, 原本就不存在返回 null</returns>
    Task<TokenSession?> DeleteAsync(string jti, CancellationToken cancellationToken = default);

    /// <summary>
    /// 查某账号的全部会话(设备列表)
    /// </summary>
    /// <param name="loginId">登录 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task<IReadOnlyList<TokenSession>> FindByLoginIdAsync(string loginId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除某账号的全部会话(踢人/封禁/单端登录)
    /// </summary>
    /// <param name="loginId">登录 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>被删除的会话数量</returns>
    Task<int> DeleteByLoginIdAsync(string loginId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 查账号封禁截止时间
    /// </summary>
    /// <param name="loginId">登录 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task<DateTimeOffset?> FindDisableAsync(string loginId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 设置账号封禁截止时间, null 表示解除
    /// </summary>
    /// <param name="loginId">登录 ID</param>
    /// <param name="until">禁用截止时间</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task SetDisableAsync(string loginId, DateTimeOffset? until, CancellationToken cancellationToken = default);
}

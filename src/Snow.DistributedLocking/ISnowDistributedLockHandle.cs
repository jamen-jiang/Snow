namespace Snow.DistributedLocking;

/// <summary>
/// 分布式锁句柄(对应 ABP 的 IAbpDistributedLockHandle): 释放即解锁一层。
/// <para>句柄是「锁还在不在自己手里」的凭据: 后端判定锁失效(键被删/被改写/续期失败)时
/// <see cref="HandleLostToken"/> 被取消, <see cref="IsLockLost"/> 变为 true。</para>
/// <para>可重入的锁在重入时返回同一个句柄, 每释放一次只减一层, 减到零才真正解锁。</para>
/// </summary>
public interface ISnowDistributedLockHandle : IAsyncDisposable
{
    /// <summary>
    /// 锁丢失信号: 后端续期失败、连接断开、锁被外力删除时取消
    /// <para>没有取消表示锁仍在手里; 业务可以把它链接进自己的取消令牌提前收手。</para>
    /// </summary>
    CancellationToken HandleLostToken { get; }

    /// <summary>
    /// 锁是否已在持锁期间丢失
    /// </summary>
    bool IsLockLost { get; }

    /// <summary>
    /// 当前调用链是否仍持有这把锁(对应 Redisson 的 <c>isHeldByCurrentThread()</c>)
    /// <para>.NET 用异步调用链(<see cref="AsyncLocal{T}"/>)而不是线程来界定「自己」, 所以成员名叫 Flow:
    /// 同一条 async 链跨线程切换仍算持有; 另起的 <see cref="Task.Run(Action)"/> 分支不算(它拿不到重入表)。</para>
    /// <para>与 <see cref="IsLockLost"/> 的区别: 后者回答「这层持有期间锁有没有被别人抢走/过期」,
    /// 本成员回答「现在这条链还剩几层没释放」。锁丢失但尚未释放时两个都可能是 true。</para>
    /// </summary>
    bool IsHeldByCurrentFlow { get; }

    /// <summary>
    /// 当前调用链对这把锁的重入层数(对应 Redisson 的 <c>getHoldCount()</c>); 0 表示已完全释放
    /// </summary>
    int HoldCount { get; }

    /// <summary>
    /// 是否支持显式续期(后端必须能延长租期才会为 true; 例如 Redis 锁用 Lua 重设 PEXPIRE 返回 true,
    /// 进程内锁没有租期概念, 返回 false)
    /// </summary>
    bool CanExtend { get; }

    /// <summary>
    /// 把锁的租期重设为 <paramref name="leaseTime"/>(注意是重设, 不是在当前剩余时间上追加)
    /// </summary>
    /// <param name="leaseTime">新的租期</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>锁仍在有效期内返回 true; 已丢失或无法延长返回 false</returns>
    Task<bool> TryExtendAsync(TimeSpan leaseTime, CancellationToken cancellationToken = default);
}

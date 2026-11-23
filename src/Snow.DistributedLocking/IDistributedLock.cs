namespace Snow.DistributedLocking;

/// <summary>
/// 分布式锁(与 ABP 的 <c>IAbpDistributedLock</c> 同形): 尝试获取一个具名锁。
/// <para>返回可释放的句柄, 建议放在 using/await using 块里; 拿不到锁时返回 null, 由调用方决定失败策略。</para>
/// <para>默认实现是进程内的 <see cref="LocalDistributedLock"/>; 引入 <c>Snow.DistributedLocking.Redis</c> 后由
/// <c>RedisDistributedLock</c>(SET NX PX + Lua)替换成多实例可用的分布式实现。</para>
/// <para>拿锁成功后由实现内部立即挂上看门狗(对应 Redisson 在 <c>lock()</c> 里启 <c>ExpirationRenewal</c>):
/// 调用方不需要关心续期, 只要在结束处释放句柄; 锁真丢了句柄的 <see cref="IDistributedLockHandle.HandleLostToken"/> 会被取消,
/// <see cref="IDistributedLockHandle.IsLockLost"/> 变 true。</para>
/// <para>可重入: 同一异步调用链里顺序嵌套地对同名锁再次 TryAcquire 会直接成功(共享同一个已持有的锁),
/// 每层各释放一次, 归零才真正解锁, 因此注解嵌套与同名锁互调不会自己锁死自己。判重入前实现会先确认锁仍在自己手里
/// (Redis 实现用一次 GET 比对 token): 显式租期到期后不会把过期记录当仍持有, 那时会重新去抢。</para>
/// <para>并行分支不算重入: 持有锁之后用 <c>Task.Run</c>/<c>Task.WhenAll</c> 起的分支再去抢同名锁会正常排队
/// (不是共享), 互斥语义不会被破坏 —— 重入按「调用链上持有的节点是否仍是该锁最内层持有者」判定。</para>
/// <para>等待语义: <c>timeout</c> 不传(或传 null)表示<b>一直等待</b>, 直到拿到锁或取消令牌触发
/// (对齐 Redisson 的 <c>lock()</c>); 只抢一次就传 <see cref="TimeSpan.Zero"/>; 最多等固定时长传正数;
/// <see cref="Timeout.InfiniteTimeSpan"/> 与不传等价。</para>
/// <para>租期语义(对齐 Redisson): 不传 <c>leaseTime</c> 时用默认租期并由看门狗续期, 业务跑多久锁就保多久;
/// 传了 <c>leaseTime</c> 就固定该租期且不续期, 到点后端自动释放。</para>
/// <para>取消语义: 传入的取消令牌被取消时返回 null 而不是抛异常, 与「没抢到锁」一致。</para>
/// </summary>
public interface IDistributedLock
{
    /// <summary>
    /// 尝试获取具名锁
    /// </summary>
    /// <param name="name">锁名称, 不可为空</param>
    /// <param name="timeout">等待上限, 默认 null 表示一直等待; 传 <see cref="TimeSpan.Zero"/> 表示不等待、抢一次;
    /// 传正数表示最多等这么久</param>
    /// <param name="leaseTime">本次持锁的租期, 传 null(默认)表示用默认租期并由看门狗自动续期;
    /// 传了值则按 Redisson 的 <c>lock(leaseTime, unit)</c> 语义固定租期、<b>不启动看门狗</b>,
    /// 到点由后端自动释放(适合「最多允许占用 N 秒」的防重场景)</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>锁句柄; 拿不到锁时返回 null</returns>
    Task<IDistributedLockHandle?> TryAcquireAsync(
        string name,
        TimeSpan? timeout = null,
        TimeSpan? leaseTime = null,
        CancellationToken cancellationToken = default);
}

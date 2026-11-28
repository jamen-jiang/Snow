namespace Snow.DistributedLocking;

/// <summary>
/// 分布式锁(与 ABP 的 <c>IAbpDistributedLock</c> 同形): 尝试获取一个具名锁。
/// <para>返回可释放的句柄, 建议放进 <c>using</c>/<c>await using</c> 块; 拿不到锁时返回 null, 由调用方决定失败策略。</para>
/// <para>默认实现是进程内的 <see cref="LocalSnowDistributedLock"/>; 由基于 Medallion 的 <c>MedallionSnowDistributedLock</c> 替换成多实例可用的分布式实现(与 ABP 一致)。</para>
/// <para>等待语义与 ABP 一致: <c>timeout</c> 为 <see cref="System.TimeSpan.Zero"/>(默认)表示只抢一次;
/// 正数表示最多等这么久; <see cref="System.Threading.Timeout.InfiniteTimeSpan"/> 表示一直等到取消。</para>
/// </summary>
public interface IDistributedLock
{
    /// <summary>
    /// 尝试获取具名锁
    /// </summary>
    /// <param name="name">锁名称, 不可为空</param>
    /// <param name="timeout">等待上限; 默认只抢一次</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>锁句柄; 拿不到锁时返回 null</returns>
    Task<IDistributedLockHandle?> TryAcquireAsync(
        string name,
        TimeSpan timeout = default,
        CancellationToken cancellationToken = default);
}
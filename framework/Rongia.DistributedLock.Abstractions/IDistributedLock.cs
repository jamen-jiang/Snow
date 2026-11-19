namespace Rongia.DistributedLock;

/// <summary>
/// 分布式锁(对应 ABP 的 IAbpDistributedLock):尝试获取一个具名锁
/// <para>返回可释放的句柄,建议放在 using/await using 块里;拿不到锁时返回 null,由调用方决定失败策略。</para>
/// <para>默认实现是进程内的 <see cref="LocalDistributedLock"/>;引入 Medallion 提供器后由
/// <c>Rongia.DistributedLock</c> 的 <see cref="DistributedLockModule"/> 替换成真正的分布式实现。</para>
/// </summary>
public interface IDistributedLock
{
    /// <summary>
    /// 尝试获取具名锁
    /// </summary>
    /// <param name="name">锁名称,不可为空</param>
    /// <param name="timeout">等待上限,默认 <see cref="TimeSpan.Zero"/> 表示不等待、立刻返回</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>锁句柄;拿不到锁时返回 null</returns>
    Task<IDistributedLockHandle?> TryAcquireAsync(
        string name,
        TimeSpan timeout = default,
        CancellationToken cancellationToken = default);
}

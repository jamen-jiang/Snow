namespace Snow.DistributedLocking;

/// <summary>
/// 不做任何加锁的空实现(对应 ABP 的 <c>NullAbpDistributedLock</c>): 永远返回一个空句柄, 用于不需要锁或测试场景。
/// </summary>
public class NullSnowDistributedLock : IDistributedLock
{
    /// <inheritdoc />
    public Task<IDistributedLockHandle?> TryAcquireAsync(
        string name,
        TimeSpan timeout = default,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IDistributedLockHandle?>(new LocalSnowDistributedLockHandle(NullDisposable.Instance));
    }
}
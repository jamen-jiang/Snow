using Snow.DependencyInjection;
using Snow.Threading;

namespace Snow.DistributedLocking;

/// <summary>
/// 进程内的分布式锁默认实现(与 ABP 的 <c>LocalAbpDistributedLock</c> 一致): 底层是 <see cref="KeyedLock"/>。
/// <para>只在单进程内互斥; 多实例部署请引用 <c>DistributedLock.Redis</c> 等后端包并注册 Medallion 的 <c>IDistributedLockProvider</c>, 由分布式实现顶替。</para>
/// </summary>
[SingletonService]
public class LocalSnowDistributedLock : IDistributedLock
{
    /// <summary>
    /// key 归一化服务
    /// </summary>
    protected IDistributedLockKeyNormalizer DistributedLockKeyNormalizer { get; }

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="distributedLockKeyNormalizer">key 归一化服务</param>
    public LocalSnowDistributedLock(IDistributedLockKeyNormalizer distributedLockKeyNormalizer)
    {
        ArgumentNullException.ThrowIfNull(distributedLockKeyNormalizer);

        DistributedLockKeyNormalizer = distributedLockKeyNormalizer;
    }

    /// <inheritdoc />
    public async Task<IDistributedLockHandle?> TryAcquireAsync(
        string name,
        TimeSpan timeout = default,
        CancellationToken cancellationToken = default)
    {
        Check.NotNullOrWhiteSpace(name, nameof(name));

        string key = DistributedLockKeyNormalizer.NormalizeKey(name);

        IDisposable? disposable = await KeyedLock
            .TryLockAsync(key, timeout, cancellationToken)
            .ConfigureAwait(false);

        return disposable is null ? null : new LocalSnowDistributedLockHandle(disposable);
    }
}

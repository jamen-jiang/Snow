using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Rongia.DependencyInjection;

namespace Rongia.DistributedLock;

/// <summary>
/// 进程内分布式锁(对应 ABP 的 LocalAbpDistributedLock):按 key 分配信号量,只在本进程内互斥。
/// <para>作为默认实现,单机部署即可工作;多实例部署时由 <c>Rongia.DistributedLock</c> 的 <c>RongiaDistributedLockModule</c> 替换为真正的分布式实现。</para>
/// <para>实现 <see cref="ISingletonDependency"/>,由约定注册自动登记为单例。</para>
/// </summary>
public class LocalDistributedLock : IDistributedLock, ISingletonDependency
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _localSyncObjects = new(StringComparer.Ordinal);

    /// <summary>
    /// 锁 key 规范化器
    /// </summary>
    protected IDistributedLockKeyNormalizer DistributedLockKeyNormalizer { get; }

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="distributedLockKeyNormalizer">锁 key 规范化器</param>
    public LocalDistributedLock(IDistributedLockKeyNormalizer distributedLockKeyNormalizer)
    {
        ArgumentNullException.ThrowIfNull(distributedLockKeyNormalizer);

        DistributedLockKeyNormalizer = distributedLockKeyNormalizer;
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async Task<IDistributedLockHandle?> TryAcquireAsync(
        string name,
        TimeSpan timeout = default,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        string key = DistributedLockKeyNormalizer.NormalizeKey(name);

        SemaphoreSlim semaphore = _localSyncObjects.GetOrAdd(key, static _ => new SemaphoreSlim(1, 1));

        bool acquired = await semaphore.WaitAsync(timeout <= TimeSpan.Zero ? TimeSpan.Zero : timeout, cancellationToken);

        if (!acquired)
        {
            return null;
        }

        return new LocalDistributedLockHandle(new SemaphoreReleaser(semaphore));
    }

    /// <summary>
    /// 归还信号量的可释放对象;重复释放只生效一次
    /// </summary>
    private sealed class SemaphoreReleaser : IDisposable
    {
        private readonly SemaphoreSlim _semaphore;
        private int _disposed;

        public SemaphoreReleaser(SemaphoreSlim semaphore)
        {
            _semaphore = semaphore;
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
            {
                _semaphore.Release();
            }
        }
    }
}

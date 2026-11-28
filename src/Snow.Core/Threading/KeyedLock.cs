using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Snow.Threading;

/// <summary>
/// 按 key 分组的异步互斥锁(对应 ABP 的 <c>KeyedLock</c>, 原样移植):
/// 同一个 key 同时只允许一个持有者, 不同 key 互不影响。
/// <para>纯进程内实现, 不需要任何后端; 与 ABP 一致不支持重入 —— 同一调用链对同一 key 再次加锁会等待。</para>
/// </summary>
public static class KeyedLock
{
    private static readonly Dictionary<object, RefCounted<SemaphoreSlim>> SemaphoreSlims = new();

    /// <summary>
    /// 获取 key 对应的锁, 一直等待直到拿到
    /// </summary>
    /// <param name="key">锁标识, 不可为空</param>
    /// <returns>释放即解锁的句柄</returns>
    public static async Task<IDisposable> LockAsync(object key)
    {
        return await LockAsync(key, CancellationToken.None).ConfigureAwait(false);
    }

    /// <summary>
    /// 获取 key 对应的锁, 一直等待直到拿到或取消
    /// </summary>
    /// <param name="key">锁标识, 不可为空</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>释放即解锁的句柄</returns>
    public static async Task<IDisposable> LockAsync(object key, CancellationToken cancellationToken)
    {
        Check.NotNull(key, nameof(key));

        var semaphore = GetOrCreate(key);

        try
        {
            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            var toDispose = DecrementRefAndMaybeRemove(key);
            toDispose?.Dispose();
            throw;
        }

        return new Releaser(key);
    }

    /// <summary>
    /// 尝试获取 key 对应的锁, 只抢一次(不等待)
    /// </summary>
    /// <param name="key">锁标识, 不可为空</param>
    /// <returns>句柄; 抢不到返回 null</returns>
    public static async Task<IDisposable?> TryLockAsync(object key)
    {
        return await TryLockAsync(key, default, CancellationToken.None).ConfigureAwait(false);
    }

    /// <summary>
    /// 尝试获取 key 对应的锁, 最多等待 <paramref name="timeout"/>
    /// </summary>
    /// <param name="key">锁标识, 不可为空</param>
    /// <param name="timeout">等待上限; <see cref="TimeSpan.Zero"/> 表示只抢一次</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>句柄; 抢不到返回 null</returns>
    public static async Task<IDisposable?> TryLockAsync(object key, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        Check.NotNull(key, nameof(key));

        var semaphore = GetOrCreate(key);
        bool acquired;

        try
        {
            if (timeout == default)
            {
                acquired = await semaphore.WaitAsync(0, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                acquired = await semaphore.WaitAsync(timeout, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            var toDispose = DecrementRefAndMaybeRemove(key);
            toDispose?.Dispose();
            throw;
        }

        if (acquired)
        {
            return new Releaser(key);
        }

        var toDisposeOnFail = DecrementRefAndMaybeRemove(key);
        toDisposeOnFail?.Dispose();

        return null;
    }

    private static SemaphoreSlim GetOrCreate(object key)
    {
        RefCounted<SemaphoreSlim> item;
        lock (SemaphoreSlims)
        {
            if (SemaphoreSlims.TryGetValue(key, out item!))
            {
                ++item.RefCount;
            }
            else
            {
                item = new RefCounted<SemaphoreSlim>(new SemaphoreSlim(1, 1));
                SemaphoreSlims[key] = item;
            }
        }

        return item.Value;
    }

    private static SemaphoreSlim? DecrementRefAndMaybeRemove(object key)
    {
        lock (SemaphoreSlims)
        {
            if (!SemaphoreSlims.TryGetValue(key, out var item))
            {
                return null;
            }

            --item.RefCount;

            if (item.RefCount == 0)
            {
                SemaphoreSlims.Remove(key);
                return item.Value;
            }

            return null;
        }
    }

    private sealed class RefCounted<T>(T value)
    {
        public int RefCount { get; set; } = 1;

        public T Value { get; } = value;
    }

    private sealed class Releaser(object key) : IDisposable
    {
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 1)
            {
                return;
            }

            var shouldDispose = false;
            SemaphoreSlim? semaphore = null;

            lock (SemaphoreSlims)
            {
                if (!SemaphoreSlims.TryGetValue(key, out var item))
                {
                    return;
                }

                --item.RefCount;

                if (item.RefCount == 0)
                {
                    SemaphoreSlims.Remove(key);
                    shouldDispose = true;
                    semaphore = item.Value;
                }
            }

            if (shouldDispose)
            {
                semaphore!.Dispose();
            }
            else
            {
                lock (SemaphoreSlims)
                {
                    if (SemaphoreSlims.TryGetValue(key, out var item))
                    {
                        item.Value.Release();
                    }
                }
            }
        }
    }
}

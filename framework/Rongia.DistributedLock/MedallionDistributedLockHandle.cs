using Medallion.Threading;

namespace Rongia.DistributedLock;

/// <summary>
/// Medallion 锁句柄(对应 ABP 的 MedallionAbpDistributedLockHandle):
/// 把 <see cref="IDistributedSynchronizationHandle"/> 包装成 <see cref="IDistributedLockHandle"/>
/// </summary>
public class MedallionDistributedLockHandle : IDistributedLockHandle
{
    /// <summary>
    /// 底层 Medallion 句柄
    /// </summary>
    public IDistributedSynchronizationHandle Handle { get; }

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="handle">Medallion 句柄</param>
    public MedallionDistributedLockHandle(IDistributedSynchronizationHandle handle)
    {
        ArgumentNullException.ThrowIfNull(handle);

        Handle = handle;
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        return Handle.DisposeAsync();
    }
}

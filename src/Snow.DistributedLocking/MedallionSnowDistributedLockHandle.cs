using Medallion.Threading;

namespace Snow.DistributedLocking;

/// <summary>
/// Medallion 锁句柄(与 ABP 的 <c>MedallionAbpDistributedLockHandle</c> 一致): 释放即释放底层同步句柄。
/// </summary>
public class MedallionSnowDistributedLockHandle : IDistributedLockHandle
{
    /// <summary>
    /// 底层 Medallion 同步句柄
    /// </summary>
    public IDistributedSynchronizationHandle Handle { get; }

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="handle">底层 Medallion 同步句柄</param>
    public MedallionSnowDistributedLockHandle(IDistributedSynchronizationHandle handle)
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

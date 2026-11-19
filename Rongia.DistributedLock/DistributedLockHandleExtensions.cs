using Medallion.Threading;

namespace Rongia.DistributedLock;

/// <summary>
/// 锁句柄扩展(对应 ABP 的 AbpDistributedLockHandleExtensions):把契约句柄还原成 Medallion 底层句柄
/// </summary>
public static class DistributedLockHandleExtensions
{
    /// <summary>
    /// 取出 Medallion 的底层同步句柄
    /// </summary>
    /// <param name="handle">锁句柄</param>
    /// <returns>Medallion 同步句柄</returns>
    /// <exception cref="ArgumentException">句柄不是 <see cref="MedallionDistributedLockHandle"/></exception>
    public static IDistributedSynchronizationHandle ToDistributedSynchronizationHandle(this IDistributedLockHandle handle)
    {
        ArgumentNullException.ThrowIfNull(handle);

        return handle is MedallionDistributedLockHandle medallionHandle
            ? medallionHandle.Handle
            : throw new ArgumentException("句柄不是 Medallion 分布式锁句柄: " + handle.GetType().FullName, nameof(handle));
    }
}

using Medallion.Threading;

namespace Snow.DistributedLocking;

/// <summary>
/// 句柄扩展(与 ABP 的 <c>AbpDistributedLockHandleExtensions</c> 一致): 从 Snow 的句柄取回 Medallion 的原生句柄。
/// </summary>
public static class SnowDistributedLockHandleExtensions
{
    /// <summary>
    /// 取底层 Medallion 同步句柄
    /// </summary>
    /// <param name="handle">Snow 的锁句柄</param>
    /// <returns>Medallion 原生句柄</returns>
    public static IDistributedSynchronizationHandle ToDistributedSynchronizationHandle(this IDistributedLockHandle handle)
    {
        ArgumentNullException.ThrowIfNull(handle);

        return handle is MedallionSnowDistributedLockHandle medallionHandle
            ? medallionHandle.Handle
            : throw new InvalidOperationException($"句柄不是 Medallion 实现: {handle.GetType().FullName}");
    }
}

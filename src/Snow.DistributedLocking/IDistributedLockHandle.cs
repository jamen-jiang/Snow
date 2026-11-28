namespace Snow.DistributedLocking;

/// <summary>
/// 分布式锁句柄(与 ABP 的 <c>IAbpDistributedLockHandle</c> 一样是空接口): 释放即解锁。
/// </summary>
public interface IDistributedLockHandle : IAsyncDisposable
{
}
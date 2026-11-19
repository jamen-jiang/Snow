namespace Rongia.DistributedLock;

/// <summary>
/// 分布式锁句柄(对应 ABP 的 IAbpDistributedLockHandle):空标记接口,释放即解锁
/// </summary>
public interface IDistributedLockHandle : IAsyncDisposable
{
}

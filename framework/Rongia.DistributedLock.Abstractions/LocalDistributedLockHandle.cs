namespace Rongia.DistributedLock;

/// <summary>
/// 进程内锁句柄(对应 ABP 的 LocalAbpDistributedLockHandle):释放时归还内部的同步对象
/// </summary>
public class LocalDistributedLockHandle : IDistributedLockHandle
{
    private readonly IDisposable _disposable;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="disposable">释放时归还同步对象的可释放对象</param>
    public LocalDistributedLockHandle(IDisposable disposable)
    {
        ArgumentNullException.ThrowIfNull(disposable);

        _disposable = disposable;
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        _disposable.Dispose();

        return default;
    }
}

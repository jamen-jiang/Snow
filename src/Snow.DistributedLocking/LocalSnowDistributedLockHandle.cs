namespace Snow.DistributedLocking;

/// <summary>
/// 进程内锁句柄(与 ABP 的 <c>LocalAbpDistributedLockHandle</c> 一致): 释放时释放底层 <see cref="IDisposable"/>。
/// </summary>
public class LocalSnowDistributedLockHandle : IDistributedLockHandle
{
    private readonly IDisposable _disposable;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="disposable">底层锁句柄</param>
    public LocalSnowDistributedLockHandle(IDisposable disposable)
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
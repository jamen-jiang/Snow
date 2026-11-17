namespace Rongia.Kernel.Application.Port.Lock;

/// <summary>
/// 分布式锁句柄
/// <para>用于异步流场景,锁的释放时机由调用方决定。</para>
/// </summary>
public interface IDistributedLockHandle
{
    /// <summary>
    /// 释放锁
    /// </summary>
    void Release();
}

namespace Rongia.Kernel.Application.Port.Lock;

/// <summary>
/// 分布式锁执行器
/// <para>对外提供两种用法:</para>
/// <list type="bullet">
/// <item><see cref="Execute(string, Action)"/>/<see cref="Execute{T}(string, Func{T})"/>:同步业务使用,
/// 有事务时锁绑定到事务提交后释放,无事务时方法返回即释放;</item>
/// <item><see cref="Acquire"/>:返回 <see cref="IDistributedLockHandle"/>,用于异步流场景,由调用方决定释放时机。</item>
/// </list>
/// <para>两种用法共用的能力:防重入、抢锁失败抛异常、持有者校验、执行上下文清理。</para>
/// </summary>
public interface IDistributedLockExecutor
{
    /// <summary>
    /// 同步执行(无返回值版本)
    /// </summary>
    /// <param name="key">锁 key</param>
    /// <param name="action">业务逻辑</param>
    void Execute(string key, Action action);

    /// <summary>
    /// 同步执行(有返回值版本):防重入判断 → 获取锁(不等待,抢不到直接抛异常) → 有事务注册事务完成回调后释放,无事务 finally 里释放
    /// </summary>
    /// <param name="key">锁 key</param>
    /// <param name="action">业务逻辑</param>
    /// <returns>业务逻辑执行结果</returns>
    /// <exception cref="System.Exception">获取锁失败/获取锁被中断时抛出</exception>
    T Execute<T>(string key, Func<T> action);

    /// <summary>
    /// 手动获取锁并返回句柄(异步流场景),释放时机由调用方决定
    /// </summary>
    /// <param name="key">锁 key</param>
    /// <returns>锁句柄</returns>
    /// <exception cref="System.Exception">获取锁失败/获取锁被中断时抛出</exception>
    IDistributedLockHandle Acquire(string key);
}

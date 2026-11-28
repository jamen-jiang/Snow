using System;
using System.Threading;
using System.Threading.Tasks;

namespace Snow.DynamicProxy;

/// <summary>
/// <see cref="ISnowInterceptionScope"/> 的默认空实现: 只负责释放构造时传入的可释放对象, 完成与失败都不做事。
/// </summary>
public class SnowInterceptionScope : ISnowInterceptionScope
{
    private readonly IDisposable? _disposable;

    /// <summary>
    /// 创建作用域
    /// </summary>
    /// <param name="disposable">随作用域一起释放的对象; 可为 <c>null</c></param>
    public SnowInterceptionScope(IDisposable? disposable = null)
    {
        _disposable = disposable;
    }

    /// <inheritdoc />
    public virtual Task CompleteAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public virtual Task FailAsync(Exception exception)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public virtual void Dispose()
    {
        _disposable?.Dispose();
    }
}

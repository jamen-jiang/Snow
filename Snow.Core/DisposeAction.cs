using System;

namespace Snow;

/// <summary>
/// 在 Dispose 时执行一个动作
/// </summary>
public class DisposeAction : IDisposable
{
    private readonly Action _action;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="action">释放时执行的动作</param>
    public DisposeAction(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        _action = action;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _action();
    }
}

/// <summary>
/// 在 Dispose 时带参数执行一个动作
/// </summary>
/// <typeparam name="T">动作参数类型</typeparam>
public class DisposeAction<T> : IDisposable
{
    private readonly Action<T> _action;

    private readonly T? _parameter;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="action">释放时执行的动作</param>
    /// <param name="parameter">动作参数</param>
    public DisposeAction(Action<T> action, T parameter)
    {
        ArgumentNullException.ThrowIfNull(action);

        _action = action;
        _parameter = parameter;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_parameter != null)
        {
            _action(_parameter);
        }
    }
}

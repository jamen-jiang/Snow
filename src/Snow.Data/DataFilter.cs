using System;
using System.Threading;
using Microsoft.Extensions.Options;
using Snow.DependencyInjection;

namespace Snow.Data;

/// <summary>
/// 单个过滤器的状态(对齐 ABP 的 <c>DataFilter&lt;TFilter&gt;</c>):
/// <para>状态存在 <see cref="AsyncLocal{T}"/> 里, 同一次异步调用链共享, 跨请求隔离。</para>
/// </summary>
/// <typeparam name="TFilter">过滤器类型</typeparam>
[SingletonService]
public class DataFilter<TFilter> : IDataFilter<TFilter>
    where TFilter : class
{
    private readonly SnowDataFilterOptions _options;

    private readonly AsyncLocal<DataFilterState?> _filter = new();

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="options">数据过滤选项</param>
    public DataFilter(IOptions<SnowDataFilterOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options.Value;
    }

    /// <inheritdoc />
    public bool IsEnabled
    {
        get
        {
            EnsureInitialized();

            return _filter.Value!.IsEnabled;
        }
    }

    /// <inheritdoc />
    public IDisposable Enable()
    {
        if (IsEnabled)
        {
            return NullDisposable.Instance;
        }

        _filter.Value!.IsEnabled = true;

        return new DisposeAction(() => Disable());
    }

    /// <inheritdoc />
    public IDisposable Disable()
    {
        if (!IsEnabled)
        {
            return NullDisposable.Instance;
        }

        _filter.Value!.IsEnabled = false;

        return new DisposeAction(() => Enable());
    }

    /// <summary>
    /// 首次访问时按配置的默认状态初始化当前异步上下文里的过滤器状态
    /// </summary>
    private void EnsureInitialized()
    {
        if (_filter.Value is not null)
        {
            return;
        }

        _filter.Value = _options.DefaultStates.TryGetValue(typeof(TFilter), out DataFilterState? state)
            ? state.Clone()
            : new DataFilterState(true);
    }
}

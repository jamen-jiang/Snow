namespace Snow.Uow;

/// <summary>
/// 默认工作单元实现: 回调链 + 共享对象容器
/// </summary>
public sealed class SnowUnitOfWork : IUnitOfWork
{
    private readonly List<Func<CancellationToken, Task>> _preCompleteCallbacks = [];
    private readonly List<Func<CancellationToken, Task>> _commitCallbacks = [];
    private readonly List<Func<Task>> _completedCallbacks = [];
    private readonly List<Func<CancellationToken, Task>> _rollbackCallbacks = [];

    private readonly IUnitOfWorkAccessor _accessor;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="serviceProvider">服务提供者</param>
    /// <param name="options">选项</param>
    /// <param name="accessor">环境工作单元访问器</param>
    public SnowUnitOfWork(IServiceProvider serviceProvider, UnitOfWorkOptions options, IUnitOfWorkAccessor accessor)
    {
        ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        Options = options ?? throw new ArgumentNullException(nameof(options));
        _accessor = accessor ?? throw new ArgumentNullException(nameof(accessor));
    }

    /// <inheritdoc />
    public Guid Id { get; } = Guid.NewGuid();

    /// <inheritdoc />
    public bool IsActive { get; private set; } = true;

    /// <inheritdoc />
    public bool IsCompleted { get; private set; }

    /// <inheritdoc />
    public UnitOfWorkOptions Options { get; }

    /// <inheritdoc />
    public IDictionary<string, object?> Items { get; } = new Dictionary<string, object?>();

    /// <inheritdoc />
    public IServiceProvider ServiceProvider { get; }

    /// <inheritdoc />
    public void OnPreComplete(Func<CancellationToken, Task> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);

        _preCompleteCallbacks.Add(callback);
    }

    /// <inheritdoc />
    public void OnCommit(Func<CancellationToken, Task> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);

        _commitCallbacks.Add(callback);
    }

    /// <inheritdoc />
    public void OnCompleted(Func<Task> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);

        _completedCallbacks.Add(callback);
    }

    /// <inheritdoc />
    public void OnRollback(Func<CancellationToken, Task> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);

        _rollbackCallbacks.Add(callback);
    }

    /// <inheritdoc />
    public async Task CompleteAsync(CancellationToken cancellationToken = default)
    {
        if (!IsActive)
        {
            return;
        }

        IsActive = false;

        try
        {
            foreach (Func<CancellationToken, Task> callback in _preCompleteCallbacks)
            {
                await callback(cancellationToken);
            }

            foreach (Func<CancellationToken, Task> callback in _commitCallbacks)
            {
                await callback(cancellationToken);
            }
        }
        catch
        {
            await RunRollbackCallbacksAsync(cancellationToken);
            ClearAmbient();

            throw;
        }

        IsCompleted = true;

        foreach (Func<Task> callback in _completedCallbacks)
        {
            await callback();
        }

        ClearAmbient();
    }

    /// <inheritdoc />
    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (!IsActive || IsCompleted)
        {
            return;
        }

        IsActive = false;

        await RunRollbackCallbacksAsync(cancellationToken);
        ClearAmbient();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        // 没显式提交就释放, 语义等于回滚, 避免未提交的数据被误当成成功
        if (IsActive)
        {
            await RollbackAsync();
        }
    }

    /// <summary>
    /// 逆序执行回滚回调
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步任务</returns>
    private async Task RunRollbackCallbacksAsync(CancellationToken cancellationToken)
    {
        for (int index = _rollbackCallbacks.Count - 1; index >= 0; index--)
        {
            await _rollbackCallbacks[index](cancellationToken);
        }
    }

    /// <summary>
    /// 清除环境工作单元
    /// </summary>
    private void ClearAmbient()
    {
        if (ReferenceEquals(_accessor.Current, this))
        {
            _accessor.SetUnitOfWork(null);
        }
    }
}

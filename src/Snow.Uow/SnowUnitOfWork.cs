using Microsoft.Extensions.DependencyInjection;

namespace Snow.Uow;

/// <summary>
/// 默认工作单元实现: 回调链 + 共享对象容器 + 待派发事件队列
/// </summary>
public sealed class SnowUnitOfWork : IUnitOfWork
{
    private readonly List<Func<CancellationToken, Task>> _preCompleteCallbacks = [];
    private readonly List<Func<CancellationToken, Task>> _commitCallbacks = [];
    private readonly List<Func<Task>> _completedCallbacks = [];
    private readonly List<Func<CancellationToken, Task>> _rollbackCallbacks = [];
    private readonly List<UnitOfWorkEventRecord> _localEvents = [];

    private readonly IUnitOfWorkAccessor _accessor;

    private bool _isCompleting;

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
    public IReadOnlyList<UnitOfWorkEventRecord> LocalEvents => _localEvents.AsReadOnly();

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
    public void AddOrReplaceLocalEvent(UnitOfWorkEventRecord eventRecord,
        Predicate<UnitOfWorkEventRecord>? replacePredicate = null)
    {
        ArgumentNullException.ThrowIfNull(eventRecord);

        if (replacePredicate is not null)
        {
            _localEvents.RemoveAll(replacePredicate);
        }

        _localEvents.Add(eventRecord);
    }

    /// <inheritdoc />
    public async Task CompleteAsync(CancellationToken cancellationToken = default)
    {
        // 派发中处理器可能又保存数据并再进来一次, 让外层循环统一收尾
        if (!IsActive || IsCompleted || _isCompleting)
        {
            return;
        }

        _isCompleting = true;

        try
        {
            await RunPreCompleteCallbacksAsync(cancellationToken);

            // 派发事件后再刷一次库(处理器可能改了数据, 也可能又攒了事件), 直到队列空。
            // 事务提交放在派发之后: 派发失败就整体回滚, 不会出现事件发了而数据没落。
            while (_localEvents.Count > 0)
            {
                UnitOfWorkEventRecord[] localEvents = [.. _localEvents.OrderBy(record => record.EventOrder)];
                _localEvents.Clear();

                await PublishLocalEventsAsync(localEvents, cancellationToken);

                await RunPreCompleteCallbacksAsync(cancellationToken);
            }

            foreach (Func<CancellationToken, Task> callback in _commitCallbacks)
            {
                await callback(cancellationToken);
            }
        }
        catch
        {
            IsActive = false;
            await RunRollbackCallbacksAsync(cancellationToken);
            ClearAmbient();

            throw;
        }

        // 提交回调跑完才算结束: 派发期间工作单元仍是活动的, 处理器登记的新事件会继续被派发
        IsActive = false;
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
    /// 顺序执行提交前回调(刷库); 回调执行中新增的回调不参与本轮
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步任务</returns>
    private async Task RunPreCompleteCallbacksAsync(CancellationToken cancellationToken)
    {
        Func<CancellationToken, Task>[] callbacks = [.. _preCompleteCallbacks];

        foreach (Func<CancellationToken, Task> callback in callbacks)
        {
            await callback(cancellationToken);
        }
    }

    /// <summary>
    /// 把事件交给工作单元事件派发器
    /// </summary>
    /// <param name="localEvents">已排序的事件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步任务</returns>
    private async Task PublishLocalEventsAsync(UnitOfWorkEventRecord[] localEvents,
        CancellationToken cancellationToken)
    {
        IUnitOfWorkEventPublisher? publisher = ServiceProvider.GetService<IUnitOfWorkEventPublisher>();
        if (publisher is null)
        {
            return;
        }

        await publisher.PublishLocalEventsAsync(localEvents, cancellationToken);
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

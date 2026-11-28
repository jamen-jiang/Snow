using Microsoft.Extensions.DependencyInjection;
using Snow.DependencyInjection;

namespace Snow.Uow;

/// <summary>
/// 默认工作单元实现(对应 ABP 的 <c>UnitOfWork</c>): 数据库/事务 API 登记表 + 提交后/回滚回调 + 待派发事件队列
/// <para>由容器按瞬态创建: 每个工作单元活在自己的 DI 作用域里, 作用域内的 Scoped 服务(含 DbContext)随它一起创建、一起销毁。</para>
/// </summary>
[TransientService]
public sealed class SnowUnitOfWork : IUnitOfWork
{
    private readonly List<Func<Task>> _completedCallbacks = [];
    private readonly List<Func<CancellationToken, Task>> _rollbackCallbacks = [];
    private readonly List<UnitOfWorkEventRecord> _localEvents = [];

    private readonly Dictionary<Type, object> _apis = [];

    private readonly IUnitOfWorkAccessor _accessor;

    private bool _isCompleting;
    private bool _isDisposed;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="serviceProvider">本工作单元的作用域服务提供者</param>
    /// <param name="accessor">环境工作单元访问器</param>
    public SnowUnitOfWork(IServiceProvider serviceProvider, IUnitOfWorkAccessor accessor)
    {
        ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _accessor = accessor ?? throw new ArgumentNullException(nameof(accessor));
    }

    /// <inheritdoc />
    public Guid Id { get; } = Guid.NewGuid();

    /// <inheritdoc />
    public bool IsActive { get; private set; } = true;

    /// <inheritdoc />
    public bool IsCompleted { get; private set; }

    /// <inheritdoc />
    public bool IsDisposed => _isDisposed;

    /// <inheritdoc />
    public UnitOfWorkOptions Options { get; private set; } = default!;

    /// <inheritdoc />
    public IUnitOfWork? Outer { get; private set; }

    /// <inheritdoc />
    public event EventHandler? Disposed;

    /// <inheritdoc />
    public IServiceProvider ServiceProvider { get; }

    /// <inheritdoc />
    public IReadOnlyList<UnitOfWorkEventRecord> LocalEvents => _localEvents.AsReadOnly();

    /// <inheritdoc />
    public void SetOuter(IUnitOfWork? outer)
    {
        Outer = outer;
    }

    /// <inheritdoc />
    public void Initialize(UnitOfWorkOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (Options is not null)
        {
            throw new InvalidOperationException("这个工作单元已经初始化过了。");
        }

        Options = options;
    }

    /// <inheritdoc />
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (object api in _apis.Values)
        {
            if (api is ISupportsSavingChanges supportsSavingChanges)
            {
                await supportsSavingChanges.SaveChangesAsync(cancellationToken);
            }
        }
    }

    /// <inheritdoc />
    public T? FindApi<T>() where T : class
    {
        return _apis.GetOrDefault(typeof(T)) as T;
    }

    /// <inheritdoc />
    public T GetOrAddApi<T>(Func<T> factory) where T : class
    {
        ArgumentNullException.ThrowIfNull(factory);

        object api = _apis.GetOrAdd(typeof(T), _ => factory());

        return (T)api;
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
            await SaveChangesAsync(cancellationToken);

            // 派发事件后再刷一次库(处理器可能改了数据, 也可能又攒了事件), 直到队列空。
            // 事务提交放在派发之后: 派发失败就整体回滚, 不会出现事件发了而数据没落。
            while (_localEvents.Count > 0)
            {
                UnitOfWorkEventRecord[] localEvents = [.. _localEvents.OrderBy(record => record.EventOrder)];
                _localEvents.Clear();

                await PublishLocalEventsAsync(localEvents, cancellationToken);

                await SaveChangesAsync(cancellationToken);
            }

            await CommitTransactionsAsync(cancellationToken);
        }
        catch
        {
            IsActive = false;
            await RollbackAllAsync(cancellationToken);
            await RunRollbackCallbacksAsync(cancellationToken);
            ClearAmbient();

            throw;
        }

        // 事务提交成功才算结束: 之后的提交后回调里工作单元已不活动
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

        await RollbackAllAsync(cancellationToken);
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
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;

        // 没显式提交就释放, 语义等于回滚, 避免未提交的数据被误当成成功
        if (IsActive)
        {
            await RollbackAsync();
        }

        DisposeTransactions();

        // 通知管理器: 恢复环境工作单元并销毁本工作单元的 DI 作用域
        Disposed?.Invoke(this, EventArgs.Empty);
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
    /// 提交所有事务 API
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步任务</returns>
    private async Task CommitTransactionsAsync(CancellationToken cancellationToken)
    {
        foreach (object api in _apis.Values)
        {
            if (api is ITransactionApi transactionApi)
            {
                await transactionApi.CommitAsync(cancellationToken);
            }
        }
    }

    /// <summary>
    /// 回滚所有支持回滚的数据库/事务 API(单个失败不打断其余)
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步任务</returns>
    private async Task RollbackAllAsync(CancellationToken cancellationToken)
    {
        foreach (object api in _apis.Values)
        {
            if (api is ISupportsRollback supportsRollback)
            {
                try
                {
                    await supportsRollback.RollbackAsync(cancellationToken);
                }
                catch
                {
                    // 回滚是尽力而为: 一个 API 失败不能挡住其它 API 回滚
                }
            }
        }
    }

    /// <summary>
    /// 销毁所有事务 API
    /// </summary>
    private void DisposeTransactions()
    {
        foreach (object api in _apis.Values)
        {
            if (api is not ITransactionApi transactionApi)
            {
                continue;
            }

            try
            {
                transactionApi.Dispose();
            }
            catch
            {
                // 销毁失败不往上抛: 释放流程必须走完
            }
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
    /// 清除环境工作单元: 有外层就还给外层, 否则清空
    /// </summary>
    private void ClearAmbient()
    {
        if (ReferenceEquals(_accessor.Current, this))
        {
            _accessor.SetUnitOfWork(Outer);
        }
    }
}

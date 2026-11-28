namespace Snow.Uow;

/// <summary>
/// 子工作单元(对应 ABP 的 <c>ChildUnitOfWork</c>): 已经处在一个工作单元里再次 Begin 时拿到的就是这个对象。
/// <para>它把所有操作转发给父工作单元, 自己绝不提交、也绝不回滚父: 提交权只属于最外层。</para>
/// <para>业务代码写 <c>using (var uow = manager.Begin())</c> 时, 释放子对象不会影响外层, 避免嵌套调用把外层提前回滚。</para>
/// </summary>
internal sealed class SnowChildUnitOfWork : IUnitOfWork
{
    private readonly IUnitOfWork _parent;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="parent">父工作单元</param>
    public SnowChildUnitOfWork(IUnitOfWork parent)
    {
        _parent = parent ?? throw new ArgumentNullException(nameof(parent));
    }

    /// <inheritdoc />
    public Guid Id => _parent.Id;

    /// <inheritdoc />
    public bool IsActive => _parent.IsActive;

    /// <inheritdoc />
    public bool IsCompleted => _parent.IsCompleted;

    /// <inheritdoc />
    public UnitOfWorkOptions Options => _parent.Options;

    /// <inheritdoc />
    public IUnitOfWork? Outer => _parent.Outer;

    /// <inheritdoc />
    public bool IsDisposed => _parent.IsDisposed;

    /// <inheritdoc />
    public event EventHandler? Disposed
    {
        add => _parent.Disposed += value;
        remove => _parent.Disposed -= value;
    }

    /// <inheritdoc />
    public IServiceProvider ServiceProvider => _parent.ServiceProvider;

    /// <inheritdoc />
    public IReadOnlyList<UnitOfWorkEventRecord> LocalEvents => _parent.LocalEvents;

    /// <inheritdoc />
    public void SetOuter(IUnitOfWork? outer) => _parent.SetOuter(outer);

    /// <inheritdoc />
    public void Initialize(UnitOfWorkOptions options) => _parent.Initialize(options);

    /// <inheritdoc />
    public T? FindApi<T>() where T : class => _parent.FindApi<T>();

    /// <inheritdoc />
    public T GetOrAddApi<T>(Func<T> factory) where T : class => _parent.GetOrAddApi(factory);

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _parent.SaveChangesAsync(cancellationToken);

    /// <inheritdoc />
    public void OnCompleted(Func<Task> callback) => _parent.OnCompleted(callback);

    /// <inheritdoc />
    public void OnRollback(Func<CancellationToken, Task> callback) => _parent.OnRollback(callback);

    /// <inheritdoc />
    public void AddOrReplaceLocalEvent(UnitOfWorkEventRecord eventRecord,
        Predicate<UnitOfWorkEventRecord>? replacePredicate = null)
    {
        _parent.AddOrReplaceLocalEvent(eventRecord, replacePredicate);
    }

    /// <summary>
    /// 子工作单元不提交: 提交权在外层, 这里什么也不做
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>已完成的任务</returns>
    public Task CompleteAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <summary>
    /// 回滚仍然落到父工作单元上(一旦回滚, 整个外层都不再提交)
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步任务</returns>
    public Task RollbackAsync(CancellationToken cancellationToken = default) => _parent.RollbackAsync(cancellationToken);

    /// <summary>
    /// 释放子对象不影响父工作单元
    /// </summary>
    public void Dispose()
    {
    }

    /// <summary>
    /// 释放子对象不影响父工作单元
    /// </summary>
    /// <returns>已完成的值任务</returns>
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

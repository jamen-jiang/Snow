namespace Snow.Uow;

/// <summary>
/// 工作单元: 一次业务操作的边界
/// <para>提供三件事: 按工作单元共享的对象容器(<see cref="Items"/>, 例如同一个 DbContext)、
/// 提交前后的回调链(保存数据 / 提交事务 / 提交后副作用), 以及回滚回调链。</para>
/// </summary>
public interface IUnitOfWork : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// 工作单元标识
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// 是否仍然处于活动状态(未提交也未回滚)
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// 是否已成功提交
    /// </summary>
    bool IsCompleted { get; }

    /// <summary>
    /// 选项
    /// </summary>
    UnitOfWorkOptions Options { get; }

    /// <summary>
    /// 工作单元内的共享对象容器, 键由写入方约定
    /// </summary>
    IDictionary<string, object?> Items { get; }

    /// <summary>
    /// 创建该工作单元的服务提供者
    /// </summary>
    IServiceProvider ServiceProvider { get; }

    /// <summary>
    /// 注册提交前回调(按注册顺序执行, 通常用于各 DbContext 的 SaveChanges)
    /// </summary>
    /// <param name="callback">回调</param>
    void OnPreComplete(Func<CancellationToken, Task> callback);

    /// <summary>
    /// 注册事务提交回调(如 DbContext 事务的 Commit)
    /// </summary>
    /// <param name="callback">回调</param>
    void OnCommit(Func<CancellationToken, Task> callback);

    /// <summary>
    /// 注册提交成功后的回调(已脱离事务, 适合派发领域事件、写缓存)
    /// </summary>
    /// <param name="callback">回调</param>
    void OnCompleted(Func<Task> callback);

    /// <summary>
    /// 注册回滚回调(按注册的逆序执行)
    /// </summary>
    /// <param name="callback">回调</param>
    void OnRollback(Func<CancellationToken, Task> callback);

    /// <summary>
    /// 提交: 依次执行提交前回调、事务提交回调、提交后回调
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步任务</returns>
    Task CompleteAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 回滚: 逆序执行回滚回调并结束工作单元
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步任务</returns>
    Task RollbackAsync(CancellationToken cancellationToken = default);
}

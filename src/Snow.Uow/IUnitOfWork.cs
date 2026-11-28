using Snow.DependencyInjection;

namespace Snow.Uow;

/// <summary>
/// 工作单元: 一次业务操作的边界
/// <para>提供三件事: 数据库/事务 API 的登记与取用、提交后与回滚回调(用于释放上下文等收尾),
/// 以及待派发的本地事件队列(提交时按顺序派发)。</para>
/// </summary>
public interface IUnitOfWork : IDisposable, IAsyncDisposable, IServiceProviderAccessor
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
    /// 外层工作单元; 最外层为 null
    /// </summary>
    IUnitOfWork? Outer { get; }

    /// <summary>
    /// 是否已释放
    /// </summary>
    bool IsDisposed { get; }

    /// <summary>
    /// 释放完成时触发: 工作单元管理器据此恢复环境工作单元并销毁本工作单元的 DI 作用域
    /// </summary>
    event EventHandler? Disposed;

    /// <summary>
    /// 设置外层工作单元
    /// </summary>
    /// <param name="outer">外层工作单元; 可为 null</param>
    void SetOuter(IUnitOfWork? outer);

    /// <summary>
    /// 初始化选项: 工作单元管理器创建后立即调用, 一个工作单元只能初始化一次
    /// </summary>
    /// <param name="options">选项</param>
    void Initialize(UnitOfWorkOptions options);

    /// <summary>
    /// 按类型查找已登记的数据库/事务 API(同一个上下文类型只会登记一个)
    /// </summary>
    /// <typeparam name="T">API 类型</typeparam>
    /// <returns>找到返回该实例, 否则 null</returns>
    T? FindApi<T>() where T : class;

    /// <summary>
    /// 按类型取或登记数据库/事务 API: 同一类型只创建一次, 之后的调用直接拿回已有实例
    /// </summary>
    /// <typeparam name="T">API 类型</typeparam>
    /// <param name="factory">尚未登记时用来创建实例的工厂</param>
    /// <returns>API 实例</returns>
    T GetOrAddApi<T>(Func<T> factory) where T : class;

    /// <summary>
    /// 保存所有支持保存的数据库 API(只刷数据, 不提交事务)
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步任务</returns>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 注册提交成功后的回调(已脱离事务, 适合释放上下文、写缓存)
    /// </summary>
    /// <param name="callback">回调</param>
    void OnCompleted(Func<Task> callback);

    /// <summary>
    /// 注册回滚回调(按注册的逆序执行)
    /// </summary>
    /// <param name="callback">回调</param>
    void OnRollback(Func<CancellationToken, Task> callback);

    /// <summary>
    /// 待派发的本地事件(按登记顺序)
    /// </summary>
    IReadOnlyList<UnitOfWorkEventRecord> LocalEvents { get; }

    /// <summary>
    /// 把领域事件登记到工作单元, 提交时按 <see cref="UnitOfWorkEventRecord.EventOrder"/> 派发
    /// </summary>
    /// <param name="eventRecord">事件记录</param>
    /// <param name="replacePredicate">命中该条件的旧记录先被替换掉, 为 null 表示只追加</param>
    void AddOrReplaceLocalEvent(UnitOfWorkEventRecord eventRecord,
        Predicate<UnitOfWorkEventRecord>? replacePredicate = null);

    /// <summary>
    /// 提交: 刷库、派发本地事件(派发后再刷一次库, 直到事件队列为空)、提交事务、执行提交后回调
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

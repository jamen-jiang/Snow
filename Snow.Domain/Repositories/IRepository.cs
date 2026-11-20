using Snow.Domain.Entities;

namespace Snow.Domain.Repositories;

/// <summary>
/// 仓储契约(对齐 ABP 的 <c>IRepository</c>):
/// <para>主键统一为 <see cref="IEntity.Id"/> 的 <see cref="long"/>, 写操作默认不自动保存, 由调用方决定提交时机。</para>
/// </summary>
/// <typeparam name="TEntity">实体类型</typeparam>
public interface IRepository<TEntity> : IReadOnlyRepository<TEntity>
    where TEntity : class, IEntity
{
    /// <summary>
    /// 插入实体
    /// </summary>
    /// <param name="entity">实体</param>
    /// <param name="autoSave">是否立即保存</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>插入后的实体</returns>
    Task<TEntity> InsertAsync(TEntity entity, bool autoSave = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量插入实体
    /// </summary>
    /// <param name="entities">实体集合</param>
    /// <param name="autoSave">是否立即保存</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步任务</returns>
    Task InsertManyAsync(IEnumerable<TEntity> entities, bool autoSave = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新实体
    /// </summary>
    /// <param name="entity">实体</param>
    /// <param name="autoSave">是否立即保存</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>更新后的实体</returns>
    Task<TEntity> UpdateAsync(TEntity entity, bool autoSave = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量更新实体
    /// </summary>
    /// <param name="entities">实体集合</param>
    /// <param name="autoSave">是否立即保存</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步任务</returns>
    Task UpdateManyAsync(IEnumerable<TEntity> entities, bool autoSave = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除实体
    /// </summary>
    /// <param name="entity">实体</param>
    /// <param name="autoSave">是否立即保存</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步任务</returns>
    Task DeleteAsync(TEntity entity, bool autoSave = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// 按主键删除实体, 不存在抛 <see cref="EntityNotFoundException"/>
    /// </summary>
    /// <param name="id">主键</param>
    /// <param name="autoSave">是否立即保存</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步任务</returns>
    Task DeleteAsync(long id, bool autoSave = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量删除实体
    /// </summary>
    /// <param name="entities">实体集合</param>
    /// <param name="autoSave">是否立即保存</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步任务</returns>
    Task DeleteManyAsync(IEnumerable<TEntity> entities, bool autoSave = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// 按主键批量删除
    /// </summary>
    /// <param name="ids">主键集合</param>
    /// <param name="autoSave">是否立即保存</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步任务</returns>
    Task DeleteManyAsync(IEnumerable<long> ids, bool autoSave = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// 立即保存上下文中的全部变更
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>受影响行数</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

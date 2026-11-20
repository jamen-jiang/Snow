using Snow.Domain.Entities;

namespace Snow.Domain.Repositories;

/// <summary>
/// 只读仓储契约(对齐 ABP 的 <c>IReadOnlyRepository</c>)
/// </summary>
/// <typeparam name="TEntity">实体类型</typeparam>
public interface IReadOnlyRepository<TEntity>
    where TEntity : class, IEntity
{
    /// <summary>
    /// 按主键查找, 不存在返回 null
    /// </summary>
    /// <param name="id">主键</param>
    /// <param name="includeDetails">是否加载导航属性</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>实体或 null</returns>
    Task<TEntity?> FindAsync(long id, bool includeDetails = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// 按主键获取, 不存在抛 <see cref="EntityNotFoundException"/>
    /// </summary>
    /// <param name="id">主键</param>
    /// <param name="includeDetails">是否加载导航属性</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>实体</returns>
    Task<TEntity> GetAsync(long id, bool includeDetails = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// 取全部实体
    /// </summary>
    /// <param name="includeDetails">是否加载导航属性</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>实体列表</returns>
    Task<List<TEntity>> GetListAsync(bool includeDetails = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// 取实体总数
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>总数</returns>
    Task<long> GetCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 取可组合的查询对象
    /// </summary>
    /// <returns>查询对象</returns>
    Task<IQueryable<TEntity>> GetQueryableAsync();
}

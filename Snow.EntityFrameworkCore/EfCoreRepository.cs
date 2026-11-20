using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Snow.Domain.Entities;
using Snow.EntityFrameworkCore;

namespace Snow.Domain.Repositories.EntityFrameworkCore;

/// <summary>
/// EF Core 仓储实现(对齐 ABP 的 <c>EfCoreRepository</c>):
/// <para>上下文按服务作用域共享, 写操作默认不保存, <c>autoSave</c> 为 true 时立即提交。</para>
/// </summary>
/// <typeparam name="TDbContext">上下文类型</typeparam>
/// <typeparam name="TEntity">实体类型</typeparam>
public class EfCoreRepository<TDbContext, TEntity> : IRepository<TEntity>
    where TDbContext : ISnowDbContext
    where TEntity : class, IEntity
{
    private readonly IDbContextProvider<TDbContext> _dbContextProvider;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="dbContextProvider">上下文提供器</param>
    public EfCoreRepository(IDbContextProvider<TDbContext> dbContextProvider)
    {
        _dbContextProvider = dbContextProvider ?? throw new ArgumentNullException(nameof(dbContextProvider));
    }

    /// <summary>
    /// 取当前作用域的上下文
    /// </summary>
    /// <returns>上下文实例</returns>
    protected virtual Task<TDbContext> GetDbContextAsync()
    {
        return _dbContextProvider.GetDbContextAsync();
    }

    /// <summary>
    /// 取底层 EF Core 上下文
    /// </summary>
    /// <returns>上下文实例</returns>
    protected virtual async Task<DbContext> GetDbContextCoreAsync()
    {
        return (await GetDbContextAsync()).DbContext;
    }

    /// <summary>
    /// 取实体集合
    /// </summary>
    /// <returns>实体集合</returns>
    protected virtual async Task<DbSet<TEntity>> GetDbSetAsync()
    {
        return (await GetDbContextCoreAsync()).Set<TEntity>();
    }

    /// <inheritdoc />
    public virtual async Task<TEntity?> FindAsync(long id, bool includeDetails = true, CancellationToken cancellationToken = default)
    {
        return await (await GetDbSetAsync()).FindAsync([id], cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<TEntity> GetAsync(long id, bool includeDetails = true, CancellationToken cancellationToken = default)
    {
        return await FindAsync(id, includeDetails, cancellationToken)
               ?? throw new EntityNotFoundException(typeof(TEntity), id);
    }

    /// <inheritdoc />
    public virtual async Task<List<TEntity>> GetListAsync(bool includeDetails = false, CancellationToken cancellationToken = default)
    {
        return await (await GetDbSetAsync()).ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<long> GetCountAsync(CancellationToken cancellationToken = default)
    {
        return await (await GetDbSetAsync()).LongCountAsync(cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<IQueryable<TEntity>> GetQueryableAsync()
    {
        return (await GetDbSetAsync()).AsQueryable();
    }

    /// <inheritdoc />
    public virtual async Task<TEntity> InsertAsync(TEntity entity, bool autoSave = false, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        DbContext dbContext = await GetDbContextCoreAsync();
        EntityEntry<TEntity> entry = await dbContext.Set<TEntity>().AddAsync(entity, cancellationToken);

        if (autoSave)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return entry.Entity;
    }

    /// <inheritdoc />
    public virtual async Task InsertManyAsync(IEnumerable<TEntity> entities, bool autoSave = false, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entities);

        TEntity[] entityArray = entities.ToArray();
        if (entityArray.Length == 0)
        {
            return;
        }

        DbContext dbContext = await GetDbContextCoreAsync();
        await dbContext.Set<TEntity>().AddRangeAsync(entityArray, cancellationToken);

        if (autoSave)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    /// <inheritdoc />
    public virtual async Task<TEntity> UpdateAsync(TEntity entity, bool autoSave = false, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        DbContext dbContext = await GetDbContextCoreAsync();
        EntityEntry<TEntity> entry = dbContext.Set<TEntity>().Update(entity);

        if (autoSave)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return entry.Entity;
    }

    /// <inheritdoc />
    public virtual async Task UpdateManyAsync(IEnumerable<TEntity> entities, bool autoSave = false, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entities);

        TEntity[] entityArray = entities.ToArray();
        if (entityArray.Length == 0)
        {
            return;
        }

        DbContext dbContext = await GetDbContextCoreAsync();
        dbContext.Set<TEntity>().UpdateRange(entityArray);

        if (autoSave)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    /// <inheritdoc />
    public virtual async Task DeleteAsync(TEntity entity, bool autoSave = false, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        DbContext dbContext = await GetDbContextCoreAsync();
        dbContext.Set<TEntity>().Remove(entity);

        if (autoSave)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    /// <inheritdoc />
    public virtual async Task DeleteAsync(long id, bool autoSave = false, CancellationToken cancellationToken = default)
    {
        TEntity entity = await GetAsync(id, true, cancellationToken);

        await DeleteAsync(entity, autoSave, cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task DeleteManyAsync(IEnumerable<TEntity> entities, bool autoSave = false, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entities);

        TEntity[] entityArray = entities.ToArray();
        if (entityArray.Length == 0)
        {
            return;
        }

        DbContext dbContext = await GetDbContextCoreAsync();
        dbContext.Set<TEntity>().RemoveRange(entityArray);

        if (autoSave)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    /// <inheritdoc />
    public virtual async Task DeleteManyAsync(IEnumerable<long> ids, bool autoSave = false, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ids);

        long[] idArray = ids.ToArray();
        if (idArray.Length == 0)
        {
            return;
        }

        DbSet<TEntity> dbSet = await GetDbSetAsync();
        List<TEntity> entities = await dbSet.Where(entity => idArray.Contains(entity.Id)).ToListAsync(cancellationToken);

        await DeleteManyAsync(entities, autoSave, cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await (await GetDbContextCoreAsync()).SaveChangesAsync(cancellationToken);
    }
}

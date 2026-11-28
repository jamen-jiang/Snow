using Snow.Uow;

namespace Snow.EntityFrameworkCore;

/// <summary>
/// EF Core 数据库 API(对应 ABP 的 <c>EfCoreDatabaseApi&lt;TDbContext&gt;</c>): 把上下文交给工作单元统一保存
/// </summary>
/// <typeparam name="TDbContext">上下文类型</typeparam>
public class EfCoreDatabaseApi<TDbContext> : ISupportsSavingChanges
    where TDbContext : class, ISnowDbContext
{
    /// <summary>
    /// 上下文实例
    /// </summary>
    public TDbContext DbContext { get; }

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="dbContext">上下文实例</param>
    public EfCoreDatabaseApi(TDbContext dbContext)
    {
        DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    /// <inheritdoc />
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // 没有变更就不必惊动数据库
        if (DbContext.DbContext.ChangeTracker.HasChanges())
        {
            await DbContext.DbContext.SaveChangesAsync(cancellationToken);
        }
    }
}

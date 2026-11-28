using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Snow.Uow;

namespace Snow.EntityFrameworkCore;

/// <summary>
/// EF Core 事务 API(对应 ABP 的 <c>EfCoreTransactionApi</c>): 把底层事务交给工作单元统一提交与回滚
/// </summary>
/// <typeparam name="TDbContext">开启该事务的上下文类型</typeparam>
public class EfCoreTransactionApi<TDbContext> : ITransactionApi, ISupportsRollback
    where TDbContext : class, ISnowDbContext
{
    /// <summary>
    /// 底层数据库事务
    /// </summary>
    public IDbContextTransaction DbContextTransaction { get; }

    /// <summary>
    /// 开启该事务的上下文
    /// </summary>
    public DbContext StarterDbContext { get; }

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="dbContextTransaction">底层数据库事务</param>
    /// <param name="starterDbContext">开启该事务的上下文</param>
    public EfCoreTransactionApi(IDbContextTransaction dbContextTransaction, DbContext starterDbContext)
    {
        DbContextTransaction = dbContextTransaction ?? throw new ArgumentNullException(nameof(dbContextTransaction));
        StarterDbContext = starterDbContext ?? throw new ArgumentNullException(nameof(starterDbContext));
    }

    /// <inheritdoc />
    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        await DbContextTransaction.CommitAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        await DbContextTransaction.RollbackAsync(cancellationToken);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        DbContextTransaction.Dispose();
    }
}

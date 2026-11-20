using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Snow.Uow;

namespace Snow.EntityFrameworkCore;

/// <summary>
/// DbContext 提供器(对齐 ABP 的 <c>UnitOfWorkDbContextProvider</c>):
/// <para>1) 每个活动的工作单元各持一个上下文, 互不串味(嵌套新开工作单元时会另建一个上下文与事务);</para>
/// <para>2) 没有工作单元时退回容器登记的作用域内单例, 只读场景不额外建实例;</para>
/// <para>3) 上下文登记到工作单元后由工作单元统一保存与提交, 工作单元结束时释放本提供器自己创建的上下文。</para>
/// </summary>
/// <typeparam name="TDbContext">上下文类型</typeparam>
public class SnowDbContextProvider<TDbContext> : IDbContextProvider<TDbContext>
    where TDbContext : class, ISnowDbContext
{
    private const string ItemsKeyPrefix = "Snow.EntityFrameworkCore.DbContext:";

    private readonly IServiceProvider _serviceProvider;
    private readonly IUnitOfWorkManager? _unitOfWorkManager;

    // 每个工作单元一个上下文: 键是工作单元实例(按引用比较), 工作单元结束时移除并释放
    private readonly Dictionary<IUnitOfWork, TDbContext> _unitOfWorkContexts = [];

    private TDbContext? _ambientDbContext;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="serviceProvider">当前作用域的服务提供者</param>
    public SnowDbContextProvider(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _unitOfWorkManager = serviceProvider.GetService<IUnitOfWorkManager>();
    }

    /// <inheritdoc />
    public TDbContext GetDbContext()
    {
        IUnitOfWork? unitOfWork = _unitOfWorkManager?.Current;

        if (unitOfWork is null || !unitOfWork.IsActive)
        {
            _ambientDbContext ??= _serviceProvider.GetRequiredService<TDbContext>();

            return _ambientDbContext;
        }

        if (_unitOfWorkContexts.TryGetValue(unitOfWork, out TDbContext? cached))
        {
            return cached;
        }

        TDbContext dbContext = CreateDbContext();
        _unitOfWorkContexts[unitOfWork] = dbContext;
        AttachToUnitOfWork(dbContext, unitOfWork);

        return dbContext;
    }

    /// <inheritdoc />
    public Task<TDbContext> GetDbContextAsync()
    {
        return Task.FromResult(GetDbContext());
    }

    /// <summary>
    /// 直接按选项工厂新建上下文(不经过容器), 由本提供器负责释放
    /// </summary>
    /// <returns>上下文实例</returns>
    private TDbContext CreateDbContext()
    {
        object options = DbContextOptionsFactory.Create(_serviceProvider, typeof(TDbContext));
        object instance = ActivatorUtilities.CreateInstance(_serviceProvider, typeof(TDbContext), options);

        if (instance is SnowDbContext snowDbContext)
        {
            snowDbContext.ApplicationServiceProvider = _serviceProvider;
        }

        return (TDbContext)instance;
    }

    /// <summary>
    /// 把上下文挂到工作单元: 登记共享对象、注册释放、开启事务、注册保存与提交回调
    /// <para>释放回调最先注册, 这样回滚时它最后执行(事务先回滚, 再释放上下文)。</para>
    /// </summary>
    /// <param name="dbContext">上下文</param>
    /// <param name="unitOfWork">工作单元</param>
    private void AttachToUnitOfWork(TDbContext dbContext, IUnitOfWork unitOfWork)
    {
        string itemsKey = ItemsKeyPrefix + typeof(TDbContext).FullName;
        if (unitOfWork.Items.ContainsKey(itemsKey))
        {
            return;
        }

        unitOfWork.Items[itemsKey] = dbContext;

        DbContext context = dbContext.DbContext;

        unitOfWork.OnCompleted(ReleaseAsync);
        unitOfWork.OnRollback(async _ => await ReleaseAsync());

        unitOfWork.OnPreComplete(async cancellationToken =>
        {
            if (context.ChangeTracker.HasChanges())
            {
                await context.SaveChangesAsync(cancellationToken);
            }
        });

        if (!unitOfWork.Options.IsTransactional)
        {
            return;
        }

        // 事务在上下文挂接时开启: 之后的读也处在同一个事务里(与 ABP 的 EfCoreTransactionApi 一致)
        context.Database.BeginTransaction();

        unitOfWork.OnCommit(async cancellationToken => await context.Database.CommitTransactionAsync(cancellationToken));

        unitOfWork.OnRollback(async cancellationToken =>
        {
            IDbContextTransaction? transaction = context.Database.CurrentTransaction;
            if (transaction is not null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }
        });

        return;

        async Task ReleaseAsync()
        {
            _unitOfWorkContexts.Remove(unitOfWork);
            await context.DisposeAsync();
        }
    }
}

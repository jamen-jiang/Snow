using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Snow.DependencyInjection;
using Snow.Uow;

namespace Snow.EntityFrameworkCore;

/// <summary>
/// DbContext 提供器(与 ABP 的 <c>UnitOfWorkDbContextProvider</c> 同名同形):
/// <para>1) 每个活动的工作单元各持一个上下文, 互不串味(嵌套新开工作单元时会另建一个上下文与事务);</para>
/// <para>2) 没有工作单元时退回容器登记的作用域内单例, 只读场景不额外建实例;</para>
/// <para>3) 上下文以 <see cref="EfCoreDatabaseApi{TDbContext}"/> 登记到工作单元, 由工作单元统一保存与提交, 工作单元结束时释放本提供器自己创建的上下文。</para>
/// </summary>
/// <typeparam name="TDbContext">上下文类型</typeparam>
[ScopedService]
public class UnitOfWorkDbContextProvider<TDbContext> : IDbContextProvider<TDbContext>
    where TDbContext : class, ISnowDbContext
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IUnitOfWorkManager? _unitOfWorkManager;

    private TDbContext? _ambientDbContext;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="serviceProvider">当前作用域的服务提供者</param>
    public UnitOfWorkDbContextProvider(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _unitOfWorkManager = serviceProvider.GetService<IUnitOfWorkManager>();
    }

    /// <inheritdoc />
    public Task<TDbContext> GetDbContextAsync()
    {
        IUnitOfWork? unitOfWork = _unitOfWorkManager?.Current;

        if (unitOfWork is null || !unitOfWork.IsActive)
        {
            _ambientDbContext ??= _serviceProvider.GetRequiredService<TDbContext>();

            return Task.FromResult(_ambientDbContext);
        }

        // 同一工作单元里同一个上下文类型只建一个: 按类型查, 命中就复用
        if (unitOfWork.FindApi<EfCoreDatabaseApi<TDbContext>>() is { } databaseApi)
        {
            return Task.FromResult(databaseApi.DbContext);
        }

        // 上下文从工作单元自己的 DI 作用域里建: 与工作单元同生共死, 作用域内的服务也一起复用
        TDbContext dbContext = CreateDbContext(unitOfWork);
        AttachToUnitOfWork(dbContext, unitOfWork);

        return Task.FromResult(dbContext);
    }

    /// <summary>
    /// 直接按选项工厂新建上下文(不经过容器), 由本提供器负责释放
    /// </summary>
    /// <param name="unitOfWork">上下文所属的工作单元</param>
    /// <returns>上下文实例</returns>
    private TDbContext CreateDbContext(IUnitOfWork unitOfWork)
    {
        IServiceProvider serviceProvider = unitOfWork.ServiceProvider;

        object options = DbContextOptionsFactory.Create(serviceProvider, typeof(TDbContext));
        object instance = ActivatorUtilities.CreateInstance(serviceProvider, typeof(TDbContext), options);

        if (instance is SnowDbContext snowDbContext)
        {
            snowDbContext.Initialize(serviceProvider);
        }

        return (TDbContext)instance;
    }

    /// <summary>
    /// 把上下文挂到工作单元: 登记数据库 API、开启事务并登记事务 API、登记释放
    /// <para>释放回调最先注册, 这样回滚时它最后执行(事务先回滚, 再释放上下文)。</para>
    /// </summary>
    /// <param name="dbContext">上下文</param>
    /// <param name="unitOfWork">工作单元</param>
    private void AttachToUnitOfWork(TDbContext dbContext, IUnitOfWork unitOfWork)
    {
        DbContext context = dbContext.DbContext;

        // 按类型登记: 同一个上下文类型重复调用也只会有一份
        unitOfWork.GetOrAddApi(() => new EfCoreDatabaseApi<TDbContext>(dbContext));

        if (unitOfWork.Options.IsTransactional)
        {
            // 事务在上下文挂接时开启: 之后的读也处在同一个事务里(与 ABP 的 EfCoreTransactionApi 一致)
            unitOfWork.GetOrAddApi(() => new EfCoreTransactionApi<TDbContext>(context.Database.BeginTransaction(), context));
        }

        unitOfWork.OnCompleted(ReleaseAsync);
        unitOfWork.OnRollback(async _ => await ReleaseAsync());

        return;

        async Task ReleaseAsync()
        {
            // 先销毁事务再释放上下文
            if (unitOfWork.FindApi<EfCoreTransactionApi<TDbContext>>() is { } transactionApi)
            {
                transactionApi.Dispose();
            }

            await context.DisposeAsync();
        }
    }
}

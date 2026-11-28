using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Snow;
using Snow.Auditing;
using Snow.Data;
using Snow.Domain.Entities;
using Snow.Domain.Events;
using Snow.EventBus;
using Snow.Exceptions;
using Snow.Security.Users;
using Snow.Uow;

namespace Snow.EntityFrameworkCore;

/// <summary>
/// 框架的 DbContext 基类(对齐 ABP 的 <c>AbpDbContext</c>):
/// <para>1) 保存前自动填充审计字段(创建人/创建时间、修改人/修改时间、删除人/删除时间);</para>
/// <para>2) 删除带软删除契约的实体时自动转为更新并写入删除审计;</para>
/// <para>3) 带软删除契约的实体自动挂全局查询过滤, 过滤掉已删除数据;</para>
/// <para>4) 保存前采集聚合根上挂的领域事件, 有工作单元时交给工作单元在提交事务之前统一派发,
///    没有工作单元时保存成功后立即派发;</para>
/// <para>5) 并发更新冲突(<see cref="DbUpdateConcurrencyException"/>)统一翻译为 <see cref="SnowDbConcurrencyException"/>, 由异常处理层映射为 409。</para>
/// <para>表映射与 DbSet 由具体上下文自己声明。</para>
/// </summary>
public abstract class SnowDbContext : DbContext, ISnowDbContext
{
    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="options">EF Core 选项</param>
    protected SnowDbContext(DbContextOptions options)
        : base(options)
    {
    }

    /// <inheritdoc />
    DbContext ISnowDbContext.DbContext => this;

    private ICurrentUser? _currentUser;
    private IDataFilter<ISoftDelete>? _softDeleteFilter;
    private IUnitOfWorkManager? _unitOfWorkManager;
    private IUnitOfWorkEventPublisher? _eventPublisher;
    private IEventBus? _eventBus;
    private bool _initialized;

    /// <summary>
    /// 由框架在创建上下文后调用一次: 把需要的应用服务解析好存下来, 上下文本身不持有容器
    /// </summary>
    /// <param name="serviceProvider">当前作用域的服务提供者</param>
    internal void Initialize(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        _currentUser = serviceProvider.GetService<ICurrentUser>();
        _softDeleteFilter = serviceProvider.GetService<IDataFilter<ISoftDelete>>();
        _unitOfWorkManager = serviceProvider.GetService<IUnitOfWorkManager>();
        _eventPublisher = serviceProvider.GetService<IUnitOfWorkEventPublisher>();
        _eventBus = serviceProvider.GetService<IEventBus>();
        _initialized = true;
    }

    /// <summary>
    /// 确认上下文由 Snow 创建并已完成依赖初始化
    /// </summary>
    private void EnsureInitialized()
    {
        if (!_initialized)
        {
            throw new InvalidOperationException($"{GetType().Name}: 上下文未经 Snow 创建, 缺少应用服务。请通过 Snow 模块注册或 UnitOfWorkDbContextProvider 创建上下文。");
        }
    }

    /// <summary>
    /// 软删除过滤是否启用: 业务代码用 <c>IDataFilter&lt;ISoftDelete&gt;.Disable()</c> 临时关掉它查回收站数据
    /// </summary>
    protected virtual bool IsSoftDeleteFilterEnabled
    {
        get
        {
            EnsureInitialized();
            return _softDeleteFilter?.IsEnabled ?? true;
        }
    }

    /// <summary>
    /// 审计时间来源, 子类可重写以接管时钟
    /// </summary>
    /// <returns>当前时间</returns>
    protected virtual DateTime GetAuditTime()
    {
        return DateTime.Now;
    }

    /// <inheritdoc />
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyBeforeSaveConventions();

        // 事件在写库之前采集并清空(与 ABP 的 AbpDbContext.CreateEventReport 一致)
        List<UnitOfWorkEventRecord> domainEvents = CollectDomainEvents();

        int result;
        try
        {
            result = base.SaveChanges(acceptAllChangesOnSuccess);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new SnowDbConcurrencyException(ex.Message, ex);
        }

        // 同步入口只能阻塞等待异步处理器
        DispatchDomainEventsAsync(domainEvents, CancellationToken.None).GetAwaiter().GetResult();

        return result;
    }

    /// <inheritdoc />
    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ApplyBeforeSaveConventions();

        List<UnitOfWorkEventRecord> domainEvents = CollectDomainEvents();

        int result;
        try
        {
            result = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new SnowDbConcurrencyException(ex.Message, ex);
        }

        await DispatchDomainEventsAsync(domainEvents, cancellationToken);

        return result;
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        base.OnModelCreating(modelBuilder);
        ConfigureDomainEventsIgnored(modelBuilder);
        ConfigureSoftDeleteQueryFilters(modelBuilder);
        ConfigureByConvention(modelBuilder);
    }

    /// <summary>
    /// 约定式建模扩展点: 子类可在此统一改写表名、列类型等约定
    /// </summary>
    /// <param name="modelBuilder">模型构建器</param>
    protected virtual void ConfigureByConvention(ModelBuilder modelBuilder)
    {
    }

    /// <summary>
    /// 保存前的约定处理: 审计字段与软删除
    /// </summary>
    protected virtual void ApplyBeforeSaveConventions()
    {
        EnsureInitialized();

        ICurrentUser? currentUser = _currentUser;
        DateTime now = GetAuditTime();
        long? userId = currentUser?.Id;
        string userName = ResolveUserName(currentUser);

        // 转成列表: 软删除会把 Deleted 改成 Modified, 边遍历边改状态不安全
        foreach (EntityEntry entry in ChangeTracker.Entries().ToList())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    ApplyCreationAudit(entry, userId, userName, now);
                    break;

                case EntityState.Modified:
                    ApplyModificationAudit(entry, userId, userName, now);
                    break;

                case EntityState.Deleted:
                    ApplyDeletionAudit(entry, userId, userName, now);
                    break;
            }
        }
    }

    /// <summary>
    /// 新建实体: 补创建审计, 并保证新数据的删除标记为未删除
    /// </summary>
    /// <param name="entry">实体条目</param>
    /// <param name="userId">当前用户Id</param>
    /// <param name="userName">当前用户名称</param>
    /// <param name="now">当前时间</param>
    protected virtual void ApplyCreationAudit(EntityEntry entry, long? userId, string userName, DateTime now)
    {
        if (entry.Entity is ICreationAudited creationAudited)
        {
            if (creationAudited.CreationTime == default)
            {
                SetAuditValue(entry, nameof(ICreationAudited.CreationTime), now);
            }

            if (userId.HasValue)
            {
                SetAuditValue(entry, nameof(ICreationAudited.CreatorId), userId.Value);
            }

            if (!string.IsNullOrEmpty(userName))
            {
                SetAuditValue(entry, nameof(ICreationAudited.Creator), userName);
            }
        }

        if (entry.Entity is ISoftDelete)
        {
            SetAuditValue(entry, nameof(ISoftDelete.IsDeleted), false);
        }
    }

    /// <summary>
    /// 修改实体: 补最后修改审计
    /// </summary>
    /// <param name="entry">实体条目</param>
    /// <param name="userId">当前用户Id</param>
    /// <param name="userName">当前用户名称</param>
    /// <param name="now">当前时间</param>
    protected virtual void ApplyModificationAudit(EntityEntry entry, long? userId, string userName, DateTime now)
    {
        if (entry.Entity is not IModificationAudited modificationAudited)
        {
            return;
        }

        SetAuditValue(entry, nameof(IModificationAudited.LastModificationTime), now);

        if (userId.HasValue)
        {
            SetAuditValue(entry, nameof(IModificationAudited.LastModifierId), userId);
        }

        if (!string.IsNullOrEmpty(userName))
        {
            SetAuditValue(entry, nameof(IModificationAudited.LastModifier), userName);
        }
    }

    /// <summary>
    /// 删除实体: 带软删除契约的把删除改成更新并置删除标记, 其余保持物理删除
    /// </summary>
    /// <param name="entry">实体条目</param>
    /// <param name="userId">当前用户Id</param>
    /// <param name="userName">当前用户名称</param>
    /// <param name="now">当前时间</param>
    protected virtual void ApplyDeletionAudit(EntityEntry entry, long? userId, string userName, DateTime now)
    {
        if (entry.Entity is not ISoftDelete)
        {
            return;
        }

        entry.State = EntityState.Modified;

        SetAuditValue(entry, nameof(ISoftDelete.IsDeleted), true);

        if (entry.Entity is IModificationAudited)
        {
            SetAuditValue(entry, nameof(IModificationAudited.LastModificationTime), now);

            if (userId.HasValue)
            {
                SetAuditValue(entry, nameof(IModificationAudited.LastModifierId), userId);
            }

            if (!string.IsNullOrEmpty(userName))
            {
                SetAuditValue(entry, nameof(IModificationAudited.LastModifier), userName);
            }
        }
    }

    /// <summary>
    /// 写审计字段: 审计契约只暴露 getter, 实际值经 EF 条目写回实体属性(走 setter 或 backing field)
    /// </summary>
    /// <param name="entry">实体条目</param>
    /// <param name="propertyName">属性名</param>
    /// <param name="value">要写入的值</param>
    private static void SetAuditValue(EntityEntry entry, string propertyName, object? value)
    {
        entry.Property(propertyName).CurrentValue = value;
    }

    /// <summary>
    /// 采集聚合根上挂的领域事件并清空(写库之前调用)
    /// </summary>
    /// <returns>按采集顺序排队的事件记录</returns>
    protected virtual List<UnitOfWorkEventRecord> CollectDomainEvents()
    {
        List<UnitOfWorkEventRecord> events = [];

        foreach (EntityEntry entry in ChangeTracker.Entries())
        {
            if (entry.Entity is not AggregateRoot aggregateRoot || aggregateRoot.DomainEvents.Count == 0)
            {
                continue;
            }

            foreach (IDomainEvent domainEvent in aggregateRoot.DomainEvents)
            {
                events.Add(new UnitOfWorkEventRecord(domainEvent.GetType(), domainEvent, EventOrderGenerator.GetNext()));
            }

            aggregateRoot.ClearDomainEvents();
        }

        return events;
    }

    /// <summary>
    /// 派发领域事件: 处在工作单元里就登记进队列, 由工作单元在提交事务之前统一派发;
    /// 没有工作单元时立即派发
    /// </summary>
    /// <param name="domainEvents">已采集的事件记录</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步任务</returns>
    protected virtual async Task DispatchDomainEventsAsync(IReadOnlyList<UnitOfWorkEventRecord> domainEvents,
        CancellationToken cancellationToken)
    {
        if (domainEvents.Count == 0)
        {
            return;
        }

        EnsureInitialized();

        IUnitOfWork? unitOfWork = _unitOfWorkManager?.Current;
        if (unitOfWork is not null)
        {
            foreach (UnitOfWorkEventRecord domainEvent in domainEvents)
            {
                unitOfWork.AddOrReplaceLocalEvent(domainEvent);
            }

            return;
        }

        IUnitOfWorkEventPublisher? publisher = _eventPublisher;
        if (publisher is not null)
        {
            await publisher.PublishLocalEventsAsync(domainEvents, cancellationToken);

            return;
        }

        IEventBus? eventBus = _eventBus;
        if (eventBus is null)
        {
            return;
        }

        foreach (UnitOfWorkEventRecord domainEvent in domainEvents)
        {
            await eventBus.PublishAsync(domainEvent.EventType, domainEvent.EventData, cancellationToken);
        }
    }

    /// <summary>
    /// 把聚合根的领域事件列表排除在映射之外
    /// </summary>
    /// <param name="modelBuilder">模型构建器</param>
    protected virtual void ConfigureDomainEventsIgnored(ModelBuilder modelBuilder)
    {
        foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(AggregateRoot).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType).Ignore(nameof(AggregateRoot.DomainEvents));
            }
        }
    }

    /// <summary>
    /// 给带软删除契约的实体挂全局查询过滤: 默认查询不返回已删除数据
    /// </summary>
    /// <param name="modelBuilder">模型构建器</param>
    protected virtual void ConfigureSoftDeleteQueryFilters(ModelBuilder modelBuilder)
    {
        foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes())
        {
            // 每个实体类型只遍历一次, 所以不需要判断是否已有过滤器
            if (entityType.IsOwned() || !typeof(ISoftDelete).IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }

            // 用 EF.Property 读接口成员: 直接访问接口属性在部分提供程序上无法翻译成 SQL
            ParameterExpression parameter = Expression.Parameter(entityType.ClrType, "entity");
            MethodCallExpression property = Expression.Call(
                typeof(EF),
                nameof(EF.Property),
                [typeof(bool)],
                parameter,
                Expression.Constant(nameof(ISoftDelete.IsDeleted)));

            // 过滤条件写成 (!开关 || !IsDeleted): 开关取自上下文属性, EF 每次查询重新求值, 所以 Disable/Enable 立即生效;
            // 关掉开关时整个条件恒真, 已删除行才会重新出现(对齐 ABP 的 AddSoftDeleteQueryFilter)
            Expression filterEnabled = Expression.Property(Expression.Constant(this), nameof(IsSoftDeleteFilterEnabled));

            entityType.SetQueryFilter(Expression.Lambda(Expression.OrElse(Expression.Not(filterEnabled), Expression.Not(property)), parameter));
        }
    }

    /// <summary>
    /// 取用于审计的用户名称: 优先姓名, 回退登录名
    /// </summary>
    /// <param name="currentUser">当前用户</param>
    /// <returns>用户名称</returns>
    protected virtual string ResolveUserName(ICurrentUser? currentUser)
    {
        if (currentUser is null)
        {
            return string.Empty;
        }

        if (!currentUser.Name.IsNullOrEmpty())
        {
            return currentUser.Name;
        }

        return currentUser.UserName ?? string.Empty;
    }
}

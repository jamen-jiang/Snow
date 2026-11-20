using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Snow.Data;
using Snow.Domain.Entities;
using Snow.Domain.Entities.Auditing;
using Snow.Domain.Events;
using Snow.EventBus;
using Snow.Security.Users;

namespace Snow.EntityFrameworkCore;

/// <summary>
/// 框架的 DbContext 基类(对齐 ABP 的 <c>AbpDbContext</c>):
/// <para>1) 保存前自动填充审计字段(创建人/创建时间、修改人/修改时间、删除人/删除时间);</para>
/// <para>2) 删除带软删除契约的实体时自动转为更新并写入删除审计;</para>
/// <para>3) 带软删除契约的实体自动挂全局查询过滤, 过滤掉已删除数据;</para>
/// <para>4) 保存成功后派发聚合根上挂的领域事件;</para>
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

    /// <summary>
    /// 框架创建上下文时注入的应用服务提供者(EF Core 自带的内部提供者里没有业务服务)
    /// </summary>
    internal IServiceProvider? ApplicationServiceProvider { get; set; }

    /// <summary>
    /// 当前服务提供者(取 <see cref="ICurrentUser"/> 与 <see cref="IEventBus"/> 用)
    /// </summary>
    protected IServiceProvider ServiceProvider => ApplicationServiceProvider ?? ((IInfrastructure<IServiceProvider>)this).Instance;

    /// <summary>
    /// 软删除过滤是否启用: 业务代码用 <c>IDataFilter&lt;ISoftDelete&gt;.Disable()</c> 临时关掉它查回收站数据
    /// </summary>
    protected virtual bool IsSoftDeleteFilterEnabled
        => ServiceProvider.GetService<IDataFilter<ISoftDelete>>()?.IsEnabled ?? true;

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

        int result = base.SaveChanges(acceptAllChangesOnSuccess);

        // 领域事件在保存成功后派发; 同步入口只能阻塞等待异步处理器
        PublishDomainEventsAsync(CancellationToken.None).GetAwaiter().GetResult();

        return result;
    }

    /// <inheritdoc />
    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ApplyBeforeSaveConventions();

        int result = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);

        await PublishDomainEventsAsync(cancellationToken);

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
        ICurrentUser? currentUser = ServiceProvider.GetService<ICurrentUser>();
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
                creationAudited.CreationTime = now;
            }

            if (userId.HasValue)
            {
                creationAudited.CreatorId = userId.Value;
            }

            if (!string.IsNullOrEmpty(userName))
            {
                creationAudited.Creator = userName;
            }
        }

        if (entry.Entity is ISoftDelete softDelete)
        {
            softDelete.IsDeleted = false;
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

        modificationAudited.LastModificationTime = now;

        if (userId.HasValue)
        {
            modificationAudited.LastModifierId = userId;
        }

        if (!string.IsNullOrEmpty(userName))
        {
            modificationAudited.LastModifier = userName;
        }
    }

    /// <summary>
    /// 删除实体: 带软删除契约的把删除改成更新并写删除审计, 其余保持物理删除
    /// </summary>
    /// <param name="entry">实体条目</param>
    /// <param name="userId">当前用户Id</param>
    /// <param name="userName">当前用户名称</param>
    /// <param name="now">当前时间</param>
    protected virtual void ApplyDeletionAudit(EntityEntry entry, long? userId, string userName, DateTime now)
    {
        if (entry.Entity is not IDeletionAudited deletionAudited)
        {
            return;
        }

        entry.State = EntityState.Modified;

        deletionAudited.IsDeleted = true;
        deletionAudited.DeletionTime = now;

        if (userId.HasValue)
        {
            deletionAudited.DeleterId = userId;
        }

        if (!string.IsNullOrEmpty(userName))
        {
            deletionAudited.Deleter = userName;
        }

        if (entry.Entity is IModificationAudited modificationAudited)
        {
            modificationAudited.LastModificationTime = now;

            if (userId.HasValue)
            {
                modificationAudited.LastModifierId = userId;
            }

            if (!string.IsNullOrEmpty(userName))
            {
                modificationAudited.LastModifier = userName;
            }
        }
    }

    /// <summary>
    /// 保存成功后派发聚合根上挂的领域事件
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步任务</returns>
    protected virtual async Task PublishDomainEventsAsync(CancellationToken cancellationToken = default)
    {
        List<IDomainEvent> events = [];

        foreach (EntityEntry entry in ChangeTracker.Entries())
        {
            if (entry.Entity is not AggregateRoot aggregateRoot || aggregateRoot.DomainEvents.Count == 0)
            {
                continue;
            }

            events.AddRange(aggregateRoot.DomainEvents);
            aggregateRoot.ClearDomainEvents();
        }

        if (events.Count == 0)
        {
            return;
        }

        IEventBus? eventBus = ServiceProvider.GetService<IEventBus>();
        if (eventBus is null)
        {
            return;
        }

        foreach (IDomainEvent @event in events)
        {
            await eventBus.PublishAsync(@event, cancellationToken);
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

        if (!string.IsNullOrEmpty(currentUser.Name))
        {
            return currentUser.Name!;
        }

        return currentUser.UserName ?? string.Empty;
    }
}

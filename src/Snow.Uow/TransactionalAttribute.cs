using Microsoft.Extensions.DependencyInjection;
using Snow.Aop;

namespace Snow.Uow;

/// <summary>
/// 声明式事务(对齐 Spring 的 <c>@Transactional</c>): 标在类或方法上, 被拦截的调用自动运行在一个工作单元里。
/// <para>方法正常返回即提交, 抛异常即回滚; 嵌套调用复用最外层的工作单元, 只有最外层提交。</para>
/// <para>用法: <c>[Transactional] public virtual async Task CreateAsync(...)</c> 或标在接口实现类上。</para>
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class TransactionalAttribute : SnowInterceptorAttribute
{
    /// <summary>
    /// 是否开启数据库事务(默认开启); 设为 false 表示只借用工作单元做统一保存, 不包事务
    /// </summary>
    public bool IsTransactional { get; set; } = true;

    /// <summary>
    /// 是否忽略当前工作单元强制新开一个(新开的那层自己提交)
    /// </summary>
    public bool RequiresNew { get; set; }

    /// <inheritdoc />
    public override async Task InvokeAsync(AspectContext context, AspectDelegate next, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        IUnitOfWorkManager unitOfWorkManager = context.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();

        IUnitOfWork? parent = unitOfWorkManager.Current;
        bool ownsUnitOfWork = parent is null || !parent.IsActive || RequiresNew;

        IUnitOfWork unitOfWork = unitOfWorkManager.Begin(new UnitOfWorkOptions
        {
            IsTransactional = IsTransactional,
            RequiresNew = RequiresNew,
        });

        try
        {
            await next(context, cancellationToken);

            // 只有最外层提交: 嵌套调用把提交权交给调用方
            if (ownsUnitOfWork)
            {
                await unitOfWork.CompleteAsync();
            }
        }
        catch
        {
            if (ownsUnitOfWork)
            {
                await unitOfWork.RollbackAsync();
            }

            throw;
        }
        finally
        {
            if (ownsUnitOfWork)
            {
                await unitOfWork.DisposeAsync();

                // 强制新开时把环境工作单元还给外层
                if (parent is not null)
                {
                    context.ServiceProvider.GetService<IUnitOfWorkAccessor>()?.SetUnitOfWork(parent);
                }
            }
        }
    }
}

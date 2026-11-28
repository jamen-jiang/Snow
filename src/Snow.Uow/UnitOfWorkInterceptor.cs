using Microsoft.Extensions.DependencyInjection;
using Snow.DependencyInjection;
using Snow.DynamicProxy;

namespace Snow.Uow;

/// <summary>
/// 工作单元拦截器(对应 ABP 的 <c>UnitOfWorkInterceptor</c>): 方法或类型上标了 <see cref="UnitOfWorkAttribute"/>,
/// 就为这次调用开一个工作单元。
/// </summary>
[TransientService]
public sealed class UnitOfWorkInterceptor : SnowInterceptor
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="serviceProvider">服务提供者</param>
    public UnitOfWorkInterceptor(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public override async Task InterceptAsync(ISnowMethodInvocation invocation)
    {
        ArgumentNullException.ThrowIfNull(invocation);

        UnitOfWorkAttribute? attribute = UnitOfWorkAttribute.GetAttributeOrNull(invocation.Method);

        // 显式关掉就不开
        if (attribute is not null && attribute.IsDisabled)
        {
            await invocation.ProceedAsync();
            return;
        }

        // 没特性就不开
        if (attribute is null)
        {
            await invocation.ProceedAsync();
            return;
        }

        CancellationToken cancellationToken = invocation.GetCancellationToken();
        IUnitOfWorkManager unitOfWorkManager = _serviceProvider.GetRequiredService<IUnitOfWorkManager>();

        UnitOfWorkOptions options = new()
        {
            IsTransactional = attribute.IsTransactional,
            RequiresNew = attribute.RequiresNew,
        };

        IUnitOfWork? parent = unitOfWorkManager.Current;
        bool ownsUnitOfWork = parent is null || !parent.IsActive || options.RequiresNew;

        // 已有外层且没要求强制新开时, 这里拿到的是子对象: 提交权留在最外层
        IUnitOfWork unitOfWork = unitOfWorkManager.Begin(options);

        try
        {
            await invocation.ProceedAsync();

            // 只有最外层提交: 嵌套调用把提交权交给调用方
            if (ownsUnitOfWork)
            {
                // 正常提交: 把调用方的取消令牌透传给提交回调(CA2016)
                await unitOfWork.CompleteAsync(cancellationToken);
            }
        }
        catch
        {
            if (ownsUnitOfWork)
            {
                // 回滚往往正是由取消触发: 这里显式传 CancellationToken.None,
                // 表示「有意不传播取消令牌」, 保证事务一定能回滚/清理, 不会被已取消的令牌中途打断(CA2016)
                await unitOfWork.RollbackAsync(CancellationToken.None);
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
                    _serviceProvider.GetService<IUnitOfWorkAccessor>()?.SetUnitOfWork(parent);
                }
            }
        }
    }
}

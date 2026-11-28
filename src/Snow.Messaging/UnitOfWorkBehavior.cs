using Snow.Uow;

namespace Snow.Messaging;

/// <summary>
/// 命令工作单元行为: 对通过命令总线派发的命令自动开启事务化工作单元;
/// 查询直接透传, 不开启工作单元。
/// </summary>
public sealed class UnitOfWorkBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public UnitOfWorkBehavior(IUnitOfWorkManager unitOfWorkManager)
    {
        _unitOfWorkManager = unitOfWorkManager;
    }

    public async Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // 坑点: ICommand 与 ICommand<TResponse> 是两个互不继承的兄弟接口, 两个都要判,
        // 否则「有返回值的命令」不会被识别为命令。
        if (request is not ICommand && request is not ICommand<TResponse>)
        {
            return await next(cancellationToken);
        }

        var parent = _unitOfWorkManager.Current;
        var ownsUnitOfWork = parent is null || !parent.IsActive;

        var unitOfWork = _unitOfWorkManager.Begin(new UnitOfWorkOptions { IsTransactional = true });
        try
        {
            var result = await next(cancellationToken);
            if (ownsUnitOfWork)
            {
                await unitOfWork.CompleteAsync(cancellationToken);
            }
            return result;
        }
        catch
        {
            if (ownsUnitOfWork)
            {
                await unitOfWork.RollbackAsync(CancellationToken.None);
            }
            throw;
        }
        finally
        {
            if (ownsUnitOfWork)
            {
                await unitOfWork.DisposeAsync();
            }
        }
    }
}

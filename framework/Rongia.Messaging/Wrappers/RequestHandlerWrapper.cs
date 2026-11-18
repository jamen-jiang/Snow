using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Rongia.Messaging.Wrappers;

/// <summary>
/// 全部请求包装器的非泛型基类:让缓存可以按 <see cref="Type"/> 存放不同类型参数的包装器
/// </summary>
public abstract class RequestHandlerBase
{
    /// <summary>
    /// 以 <see cref="object"/> 形态进入,内部强转后走强类型分支
    /// </summary>
    /// <param name="request">请求实例</param>
    /// <param name="serviceProvider">服务提供者</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>装箱后的结果</returns>
    public abstract Task<object?> Handle(object request, IServiceProvider serviceProvider,
        CancellationToken cancellationToken);
}

/// <summary>
/// 有返回值命令的包装器(闭式泛型由运行时按命令类型构造)
/// </summary>
/// <typeparam name="TResponse">返回值类型</typeparam>
public abstract class RequestHandlerWrapper<TResponse> : RequestHandlerBase
{
    /// <summary>
    /// 执行命令
    /// </summary>
    /// <param name="command">命令实例</param>
    /// <param name="serviceProvider">服务提供者</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>处理结果</returns>
    public abstract Task<TResponse> Handle(ICommand<TResponse> command, IServiceProvider serviceProvider,
        CancellationToken cancellationToken);
}

/// <summary>
/// 无返回值命令的包装器
/// </summary>
public abstract class RequestHandlerWrapper : RequestHandlerBase
{
    /// <summary>
    /// 执行命令
    /// </summary>
    /// <param name="command">命令实例</param>
    /// <param name="serviceProvider">服务提供者</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>恒为 <see cref="Unit"/>.Value 的结果</returns>
    public abstract Task<Unit> Handle(ICommand command, IServiceProvider serviceProvider,
        CancellationToken cancellationToken);
}

/// <summary>
/// 查询的包装器
/// </summary>
/// <typeparam name="TResponse">返回值类型</typeparam>
public abstract class QueryRequestHandlerWrapper<TResponse> : RequestHandlerBase
{
    /// <summary>
    /// 执行查询
    /// </summary>
    /// <param name="query">查询实例</param>
    /// <param name="serviceProvider">服务提供者</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>查询结果</returns>
    public abstract Task<TResponse> Handle(IQuery<TResponse> query, IServiceProvider serviceProvider,
        CancellationToken cancellationToken);
}

/// <summary>
/// 有返回值命令的闭式包装器:解析 <see cref="ICommandHandler{TCommand,TResponse}"/> 并按注册顺序套上管道行为
/// </summary>
/// <typeparam name="TCommand">命令类型</typeparam>
/// <typeparam name="TResponse">返回值类型</typeparam>
public class RequestHandlerWrapperImpl<TCommand, TResponse> : RequestHandlerWrapper<TResponse>
    where TCommand : ICommand<TResponse>
{
    /// <inheritdoc />
    public override async Task<object?> Handle(object request, IServiceProvider serviceProvider,
        CancellationToken cancellationToken) =>
        await Handle((ICommand<TResponse>) request, serviceProvider, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public override Task<TResponse> Handle(ICommand<TResponse> command, IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        Task<TResponse> Handler(CancellationToken t = default) => serviceProvider.GetRequiredService<ICommandHandler<TCommand, TResponse>>()
            .HandleAsync((TCommand) command, t == default ? cancellationToken : t);

        return serviceProvider
            .GetServices<IPipelineBehavior<TCommand, TResponse>>()
            .Reverse()
            .Aggregate((RequestHandlerDelegate<TResponse>) Handler,
                (next, pipeline) => (t) => pipeline.HandleAsync((TCommand) command, next, t == default ? cancellationToken : t))();
    }
}

/// <summary>
/// 无返回值命令的闭式包装器
/// </summary>
/// <typeparam name="TCommand">命令类型</typeparam>
public class RequestHandlerWrapperImpl<TCommand> : RequestHandlerWrapper
    where TCommand : ICommand
{
    /// <inheritdoc />
    public override async Task<object?> Handle(object request, IServiceProvider serviceProvider,
        CancellationToken cancellationToken) =>
        await Handle((ICommand) request, serviceProvider, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public override Task<Unit> Handle(ICommand command, IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        async Task<Unit> Handler(CancellationToken t = default)
        {
            await serviceProvider.GetRequiredService<ICommandHandler<TCommand>>()
                .HandleAsync((TCommand) command, t == default ? cancellationToken : t);

            return Unit.Value;
        }

        return serviceProvider
            .GetServices<IPipelineBehavior<TCommand, Unit>>()
            .Reverse()
            .Aggregate((RequestHandlerDelegate<Unit>) Handler,
                (next, pipeline) => (t) => pipeline.HandleAsync((TCommand) command, next, t == default ? cancellationToken : t))();
    }
}

/// <summary>
/// 查询的闭式包装器
/// </summary>
/// <typeparam name="TQuery">查询类型</typeparam>
/// <typeparam name="TResponse">返回值类型</typeparam>
public class QueryRequestHandlerWrapperImpl<TQuery, TResponse> : QueryRequestHandlerWrapper<TResponse>
    where TQuery : IQuery<TResponse>
{
    /// <inheritdoc />
    public override async Task<object?> Handle(object request, IServiceProvider serviceProvider,
        CancellationToken cancellationToken) =>
        await Handle((IQuery<TResponse>) request, serviceProvider, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public override Task<TResponse> Handle(IQuery<TResponse> query, IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        Task<TResponse> Handler(CancellationToken t = default) => serviceProvider.GetRequiredService<IQueryHandler<TQuery, TResponse>>()
            .HandleAsync((TQuery) query, t == default ? cancellationToken : t);

        return serviceProvider
            .GetServices<IPipelineBehavior<TQuery, TResponse>>()
            .Reverse()
            .Aggregate((RequestHandlerDelegate<TResponse>) Handler,
                (next, pipeline) => (t) => pipeline.HandleAsync((TQuery) query, next, t == default ? cancellationToken : t))();
    }
}

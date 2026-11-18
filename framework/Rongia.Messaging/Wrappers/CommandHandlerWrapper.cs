using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Rongia.Messaging.Wrappers;

/// <summary>
/// 有返回值命令的包装器:解析 <see cref="ICommandHandler{TCommand,TResponse}"/> 并按注册顺序套上管道行为。
/// 闭式泛型由 <see cref="CommandBus"/> 在运行时按命令类型构造,并用缓存的委托调用,避免每次发送都做反射。
/// </summary>
/// <typeparam name="TCommand">命令类型</typeparam>
/// <typeparam name="TResponse">返回值类型</typeparam>
public class CommandHandlerWrapper<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    /// <summary>
    /// 执行命令
    /// </summary>
    /// <param name="command">命令实例</param>
    /// <param name="serviceProvider">服务提供者</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>处理结果</returns>
    public Task<TResponse> Handle(ICommand<TResponse> command, IServiceProvider serviceProvider,
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
/// 无返回值命令的包装器:解析 <see cref="ICommandHandler{TCommand}"/> 并按注册顺序套上管道行为
/// </summary>
/// <typeparam name="TCommand">命令类型</typeparam>
public class CommandHandlerWrapper<TCommand>
    where TCommand : ICommand
{
    /// <summary>
    /// 执行命令
    /// </summary>
    /// <param name="command">命令实例</param>
    /// <param name="serviceProvider">服务提供者</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>恒为 <see cref="Unit"/>.Value 的结果</returns>
    public Task<Unit> Handle(ICommand command, IServiceProvider serviceProvider,
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

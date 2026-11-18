using System.Threading;
using System.Threading.Tasks;

namespace Rongia.Messaging;

/// <summary>
/// 有返回值命令的处理器
/// </summary>
/// <typeparam name="TCommand">命令类型</typeparam>
/// <typeparam name="TResponse">返回值类型</typeparam>
public interface ICommandHandler<in TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    /// <summary>
    /// 处理命令
    /// </summary>
    /// <param name="command">命令实例</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>处理结果</returns>
    Task<TResponse> HandleAsync(TCommand command, CancellationToken cancellationToken);
}

/// <summary>
/// 无返回值命令的处理器
/// </summary>
/// <typeparam name="TCommand">命令类型</typeparam>
public interface ICommandHandler<in TCommand>
    where TCommand : ICommand
{
    /// <summary>
    /// 处理命令
    /// </summary>
    /// <param name="command">命令实例</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>表示处理过程的任务</returns>
    Task HandleAsync(TCommand command, CancellationToken cancellationToken);
}

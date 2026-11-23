namespace Snow.Messaging;

/// <summary>
/// 命令总线:把命令沿管道送给唯一的处理器(写侧;读侧见 <see cref="IQueryBus"/>)
/// </summary>
public interface ICommandBus
{
    /// <summary>
    /// 发送有返回值的命令
    /// </summary>
    /// <typeparam name="TResponse">返回值类型</typeparam>
    /// <param name="command">命令实例</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>处理结果</returns>
    Task<TResponse> SendAsync<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 发送无返回值的命令
    /// </summary>
    /// <typeparam name="TCommand">命令类型</typeparam>
    /// <param name="command">命令实例</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>表示发送过程的任务</returns>
    Task SendAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : ICommand;
}

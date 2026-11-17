using System.Threading;
using System.Threading.Tasks;

namespace Rongia.Mediator;

/// <summary>
/// Defines a handler for a command
/// </summary>
/// <typeparam name="TCommand">The type of command being handled</typeparam>
/// <typeparam name="TResponse">The type of response from the handler</typeparam>
public interface ICommandHandler<in TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    /// <summary>
    /// Handles a command
    /// </summary>
    /// <param name="command">The command</param>
    /// <param name="cancellationToken">An optional cancellation token</param>
    /// <returns>A task that represents the handling operation. The task result contains the handler response</returns>
    Task<TResponse> Handle(TCommand command, CancellationToken cancellationToken);
}

/// <summary>
/// Defines a handler for a command with a void response
/// </summary>
/// <typeparam name="TCommand">The type of command being handled</typeparam>
public interface ICommandHandler<in TCommand>
    where TCommand : ICommand
{
    /// <summary>
    /// Handles a command with no response
    /// </summary>
    /// <param name="command">The command</param>
    /// <param name="cancellationToken">An optional cancellation token</param>
    /// <returns>A task that represents the handling operation.</returns>
    Task Handle(TCommand command, CancellationToken cancellationToken);
}

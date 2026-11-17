using System.Threading;
using System.Threading.Tasks;

namespace Rongia.Mediator;

/// <summary>
/// Sends commands through the mediator pipeline to a single command handler.
/// The read side lives on <see cref="IQueryBus"/>.
/// </summary>
public interface ICommandBus
{
    /// <summary>
    /// Asynchronously send a command with a response to a single handler
    /// </summary>
    /// <typeparam name="TResponse">Response type</typeparam>
    /// <param name="command">Command object</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>A task that represents the send operation. The task result contains the handler response</returns>
    Task<TResponse> Send<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously send a command with no response to a single handler
    /// </summary>
    /// <typeparam name="TCommand">Command type</typeparam>
    /// <param name="command">Command object</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>A task that represents the send operation.</returns>
    Task Send<TCommand>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : ICommand;
}

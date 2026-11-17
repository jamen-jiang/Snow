using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mediator;

/// <summary>
/// Send a command or query through the mediator pipeline to be handled by a single handler.
/// </summary>
public interface ISender
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
    /// <param name="command">Command object</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>A task that represents the send operation.</returns>
    Task Send<TCommand>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : ICommand;

    /// <summary>
    /// Asynchronously send a query to a single handler
    /// </summary>
    /// <typeparam name="TResponse">Response type</typeparam>
    /// <param name="query">Query object</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>A task that represents the send operation. The task result contains the handler response</returns>
    Task<TResponse> Send<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously send an object command or query to a single handler via dynamic dispatch
    /// </summary>
    /// <param name="request">Command or query object</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>A task that represents the send operation. The task result contains the type erased handler response</returns>
    Task<object?> Send(object request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a stream via a single stream handler
    /// </summary>
    /// <typeparam name="TResponse">Response type</typeparam>
    /// <param name="request">Stream request object</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>An async enumerable representing the stream</returns>
    IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a stream via an object request to a stream handler
    /// </summary>
    /// <param name="request">Stream request object</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>An async enumerable representing the stream</returns>
    IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default);
}

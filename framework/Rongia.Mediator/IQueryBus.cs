using System.Threading;
using System.Threading.Tasks;

namespace Rongia.Mediator;

/// <summary>
/// Sends queries through the mediator pipeline to a single query handler.
/// The write side lives on <see cref="ICommandBus"/>.
/// </summary>
public interface IQueryBus
{
    /// <summary>
    /// Asynchronously query a single handler
    /// </summary>
    /// <typeparam name="TResponse">Response type</typeparam>
    /// <param name="query">Query object</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>A task that represents the query operation. The task result contains the handler response</returns>
    Task<TResponse> Query<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken = default);
}

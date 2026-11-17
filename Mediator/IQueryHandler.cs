using System.Threading;
using System.Threading.Tasks;

namespace Mediator;

/// <summary>
/// Defines a handler for a query
/// </summary>
/// <typeparam name="TQuery">The type of query being handled</typeparam>
/// <typeparam name="TResponse">The type of response from the handler</typeparam>
public interface IQueryHandler<in TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    /// <summary>
    /// Handles a query
    /// </summary>
    /// <param name="query">The query</param>
    /// <param name="cancellationToken">An optional cancellation token</param>
    /// <returns>A task that represents the handling operation. The task result contains the handler response</returns>
    Task<TResponse> Handle(TQuery query, CancellationToken cancellationToken);
}

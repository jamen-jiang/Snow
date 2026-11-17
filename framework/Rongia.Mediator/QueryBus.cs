using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Rongia.Mediator.Wrappers;

namespace Rongia.Mediator;

/// <summary>
/// Default <see cref="IQueryBus"/> implementation relying on cached wrappers for resolving query handlers.
/// </summary>
public class QueryBus : IQueryBus
{
    private static readonly ConcurrentDictionary<Type, RequestHandlerBase> _queryHandlers = new();

    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="QueryBus"/> class.
    /// </summary>
    /// <param name="serviceProvider">Service provider. Can be a scoped or root provider</param>
    public QueryBus(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public Task<TResponse> Query<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken = default)
    {
        if (query == null)
        {
            throw new ArgumentNullException(nameof(query));
        }

        var handler = (QueryRequestHandlerWrapper<TResponse>)_queryHandlers.GetOrAdd(query.GetType(), static queryType =>
        {
            var wrapperType = typeof(QueryRequestHandlerWrapperImpl<,>).MakeGenericType(queryType, typeof(TResponse));
            var wrapper = Activator.CreateInstance(wrapperType) ?? throw new InvalidOperationException($"Could not create wrapper type for {queryType}");
            return (RequestHandlerBase)wrapper;
        });

        return handler.Handle(query, _serviceProvider, cancellationToken);
    }
}

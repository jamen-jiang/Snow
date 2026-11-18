using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Rongia.DependencyInjection;
using Rongia.Messaging.Wrappers;

namespace Rongia.Messaging;

/// <summary>
/// <see cref="IQueryBus"/> 的默认实现:按查询类型缓存闭式包装器,避免每次查询都做反射
/// </summary>
[ExposeServices(typeof(IQueryBus))]
public class QueryBus : IQueryBus, ITransientDependency
{
    private static readonly ConcurrentDictionary<Type, RequestHandlerBase> _queryHandlers = new();

    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="serviceProvider">服务提供者,可以是作用域内或根容器</param>
    public QueryBus(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public Task<TResponse> QueryAsync<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken = default)
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

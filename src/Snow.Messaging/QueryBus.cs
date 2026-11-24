using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Snow.DependencyInjection;
using Snow.Messaging.Wrappers;

namespace Snow.Messaging;

/// <summary>
/// <see cref="IQueryBus"/> 的默认实现:按查询类型缓存包装器的调用委托,避免每次查询都做反射
/// </summary>
[TransientService]
public class QueryBus : IQueryBus
{
    private static readonly ConcurrentDictionary<Type, object> _queryHandlers = new();

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
    /// <param name="query">查询对象</param>
    /// <param name="cancellationToken">取消令牌</param>
    public Task<TResponse> QueryAsync<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken = default)
    {
        if (query == null)
        {
            throw new ArgumentNullException(nameof(query));
        }

        var handler = (Func<IQuery<TResponse>, IServiceProvider, CancellationToken, Task<TResponse>>)_queryHandlers.GetOrAdd(
            query.GetType(), static queryType =>
            {
                var wrapperType = typeof(QueryHandlerWrapper<,>).MakeGenericType(queryType, typeof(TResponse));
                object wrapper = Activator.CreateInstance(wrapperType) ?? throw new InvalidOperationException($"Could not create wrapper type for {queryType}");
                return wrapperType.GetMethod(nameof(QueryHandlerWrapper<IQuery<TResponse>, TResponse>.Handle))!
                    .CreateDelegate(typeof(Func<IQuery<TResponse>, IServiceProvider, CancellationToken, Task<TResponse>>), wrapper);
            });

        return handler(query, _serviceProvider, cancellationToken);
    }
}

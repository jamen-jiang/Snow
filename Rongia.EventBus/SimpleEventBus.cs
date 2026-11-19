using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Rongia.DependencyInjection;

namespace Rongia.EventBus;

/// <summary>
/// 进程内事件总线: 按 <c>IEventHandler&lt;TEvent&gt;</c> 从容器解析全部实现, 每个处理器独占一个作用域
/// </summary>
/// <remarks>
/// 处理器清单由容器维护, 不另存类型清单,
/// 因此订阅方只要被登记成 <c>IEventHandler&lt;TEvent&gt;</c> 就能被发布到
/// </remarks>
public sealed class SimpleEventBus : IEventBus, ISingletonDependency
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IEventHandlerPublisher _publisher;
    private readonly ConcurrentDictionary<Type, EventHandlerWrapper> _wrappers = new();

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="serviceProvider">服务提供器</param>
    /// <param name="publisher">处理器发布策略</param>
    public SimpleEventBus(IServiceProvider serviceProvider, IEventHandlerPublisher publisher)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
    }

    /// <inheritdoc />
    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : IEvent
    {
        ArgumentNullException.ThrowIfNull(@event);

        EventHandlerWrapper wrapper = _wrappers.GetOrAdd(@event.GetType(), CreateWrapper);

        return wrapper.HandleAsync(@event, _serviceProvider, PublishCoreAsync, cancellationToken);
    }

    /// <summary>
    /// 为运行期事件类型创建包装器
    /// </summary>
    /// <param name="eventType">运行期事件类型</param>
    /// <returns>包装器</returns>
    private static EventHandlerWrapper CreateWrapper(Type eventType)
    {
        Type wrapperType = typeof(EventHandlerWrapperImpl<>).MakeGenericType(eventType);

        object? instance = Activator.CreateInstance(wrapperType);

        if (instance is not EventHandlerWrapper wrapper)
        {
            throw new InvalidOperationException($"无法为事件类型 {eventType.FullName} 创建处理器包装器");
        }

        return wrapper;
    }

    /// <summary>
    /// 交给发布策略执行
    /// </summary>
    /// <param name="executors">执行单元集合</param>
    /// <param name="event">事件实例</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步任务</returns>
    private Task PublishCoreAsync(IEnumerable<EventHandlerExecutor> executors, IEvent @event, CancellationToken cancellationToken)
    {
        return _publisher.PublishAsync(executors, @event, cancellationToken);
    }
}

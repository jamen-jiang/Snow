using Microsoft.Extensions.DependencyInjection;
using Snow.DependencyInjection;
using Snow.EventBus;

namespace Snow.Uow;

/// <summary>
/// 默认事件派发器: 逐个交给 <see cref="IEventBus"/>, 没有登记事件总线时静默跳过
/// </summary>
[TransientService]
public sealed class UnitOfWorkEventPublisher : IUnitOfWorkEventPublisher
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="serviceProvider">服务提供者</param>
    public UnitOfWorkEventPublisher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <inheritdoc />
    public async Task PublishLocalEventsAsync(IEnumerable<UnitOfWorkEventRecord> localEvents,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(localEvents);

        IEventBus? eventBus = _serviceProvider.GetService<IEventBus>();
        if (eventBus is null)
        {
            return;
        }

        foreach (UnitOfWorkEventRecord localEvent in localEvents)
        {
            await eventBus.PublishAsync(localEvent.EventType, localEvent.EventData, cancellationToken);
        }
    }
}

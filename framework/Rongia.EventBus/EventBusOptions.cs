using Rongia.Collections;

namespace Rongia.EventBus;

/// <summary>
/// 事件总线配置: 只记录"哪些类型是事件处理器", 不记录实例也不负责构造
/// </summary>
public sealed class EventBusOptions
{
    /// <summary>
    /// 事件处理器类型清单;写入时校验类型确实实现了 <see cref="IEventHandler"/>
    /// </summary>
    public ITypeList<IEventHandler> Handlers { get; }

    /// <summary>
    /// 构造器
    /// </summary>
    public EventBusOptions()
    {
        Handlers = new TypeList<IEventHandler>();
    }
}

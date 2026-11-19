namespace Rongia.EventBus;

/// <summary>
/// 处理器执行单元: 把"一个处理器实例"和"如何调用它"绑在一起交给发布策略
/// </summary>
public sealed class EventHandlerExecutor
{
    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="handlerInstance">处理器实例</param>
    /// <param name="handlerCallback">调用回调</param>
    public EventHandlerExecutor(IEventHandler handlerInstance, Func<IEvent, CancellationToken, Task> handlerCallback)
    {
        HandlerInstance = handlerInstance ?? throw new ArgumentNullException(nameof(handlerInstance));
        HandlerCallback = handlerCallback ?? throw new ArgumentNullException(nameof(handlerCallback));
    }

    /// <summary>
    /// 处理器实例
    /// </summary>
    public IEventHandler HandlerInstance { get; }

    /// <summary>
    /// 调用回调
    /// </summary>
    public Func<IEvent, CancellationToken, Task> HandlerCallback { get; }
}

namespace System;

/// <summary>
/// 针对 <see cref="EventHandler"/> 的扩展方法。
/// </summary>
public static class SnowEventHandlerExtensions
{
    /// <summary>
    /// 安全地触发事件: 处理器为 null 时不抛异常。
    /// </summary>
    /// <param name="eventHandler">事件处理器</param>
    /// <param name="sender">事件源</param>
    public static void InvokeSafely(this EventHandler eventHandler, object sender)
    {
        eventHandler.InvokeSafely(sender, EventArgs.Empty);
    }

    /// <summary>
    /// 安全地触发事件: 处理器为 null 时不抛异常。
    /// </summary>
    /// <param name="eventHandler">事件处理器</param>
    /// <param name="sender">事件源</param>
    /// <param name="e">事件参数</param>
    public static void InvokeSafely(this EventHandler eventHandler, object sender, EventArgs e)
    {
        eventHandler?.Invoke(sender, e);
    }

    /// <summary>
    /// 安全地触发事件: 处理器为 null 时不抛异常。
    /// </summary>
    /// <typeparam name="TEventArgs"><see cref="EventArgs"/> 的具体类型</typeparam>
    /// <param name="eventHandler">事件处理器</param>
    /// <param name="sender">事件源</param>
    /// <param name="e">事件参数</param>
    public static void InvokeSafely<TEventArgs>(this EventHandler<TEventArgs> eventHandler, object sender, TEventArgs e)
        where TEventArgs : EventArgs
    {
        eventHandler?.Invoke(sender, e);
    }
}

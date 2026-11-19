namespace Rongia.Modularity;

/// <summary>
/// 模块关闭失败时抛出的异常(对应 ABP 的 AbpShutdownException)
/// </summary>
public class RongiaShutdownException : Exception
{
    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="message">异常消息</param>
    public RongiaShutdownException(string message)
        : base(message) { }

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="message">异常消息</param>
    /// <param name="innerException">内部异常</param>
    public RongiaShutdownException(string message, Exception innerException)
        : base(message, innerException) { }
}

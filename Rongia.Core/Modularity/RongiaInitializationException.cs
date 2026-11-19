namespace Rongia.Modularity;

/// <summary>
/// 模块初始化失败时抛出的异常(对应 ABP 的 AbpInitializationException)
/// </summary>
public class RongiaInitializationException : Exception
{
    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="message">异常消息</param>
    public RongiaInitializationException(string message)
        : base(message) { }

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="message">异常消息</param>
    /// <param name="innerException">内部异常</param>
    public RongiaInitializationException(string message, Exception innerException)
        : base(message, innerException) { }
}

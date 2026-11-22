namespace Snow.ObjectMapping;

/// <summary>
/// 对象映射异常: 映射过程出错时抛出
/// </summary>
public class SnowObjectMappingException : Exception
{
    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="message">异常消息</param>
    public SnowObjectMappingException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="message">异常消息</param>
    /// <param name="innerException">内部异常</param>
    public SnowObjectMappingException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// 构造器: 带上源类型与目标类型
    /// </summary>
    /// <param name="sourceType">源类型</param>
    /// <param name="destinationType">目标类型</param>
    /// <param name="message">异常消息</param>
    public SnowObjectMappingException(Type sourceType, Type destinationType, string message)
        : base($"无法把 {sourceType.FullName} 映射成 {destinationType.FullName}: {message}")
    {
        SourceType = sourceType;
        DestinationType = destinationType;
    }

    /// <summary>
    /// 源类型
    /// </summary>
    public Type? SourceType { get; }

    /// <summary>
    /// 目标类型
    /// </summary>
    public Type? DestinationType { get; }
}

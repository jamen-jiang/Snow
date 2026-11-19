namespace Rongia.Expressions;

/// <summary>
/// 表达式解析或求值失败
/// </summary>
public sealed class ExpressionException : Exception
{
    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="message">错误信息</param>
    public ExpressionException(string message) : base(message)
    {
    }

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="message">错误信息</param>
    /// <param name="innerException">内部异常</param>
    public ExpressionException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

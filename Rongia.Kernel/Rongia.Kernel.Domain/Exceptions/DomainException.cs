namespace Rongia.Kernel.Domain.Exceptions;

/// <summary>
/// 领域异常,业务规则违反时抛出(对应 Java DomainException)
/// </summary>
public class DomainException : Exception
{
    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="message">错误信息</param>
    public DomainException(string message) : base(message)
    {
    }
}

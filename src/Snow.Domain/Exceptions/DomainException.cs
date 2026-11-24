using Snow.Exceptions;

namespace Snow.Domain.Exceptions;

/// <summary>
/// 领域异常: 聚合根或值对象发现业务规则被违反时抛出(对应 ABP 的 <c>Volo.Abp.Domain.DomainException</c>)
/// <para>继承 <see cref="BusinessException"/>(与 ABP 一样直接继承 <see cref="System.Exception"/>, 不在框架异常树里), 于是天然带业务语义: 走 HTTP 时由异常处理层
/// 翻译成 400 + 错误码, 日志级别 Warning, 消息默认返回给调用方。领域规则被违反是预期内的失败, 不是服务器故障,
/// 所以不该按 500 与「服务器内部错误」回给客户端。</para>
/// <para>只说明"这次操作不合法"用单参构造; 需要客户端按错误码分支处理时用 <c>(code, message)</c> 构造。</para>
/// </summary>
public class DomainException : BusinessException
{
    /// <summary>
    /// 用消息构造
    /// </summary>
    /// <param name="message">错误信息, 会原样返回给调用方</param>
    public DomainException(string message)
        : base(message: message)
    {
    }

    /// <summary>
    /// 用错误码与消息构造
    /// </summary>
    /// <param name="code">业务错误码, 客户端据此分支处理</param>
    /// <param name="message">错误信息, 会原样返回给调用方</param>
    public DomainException(string code, string message)
        : base(code: code, message: message)
    {
    }

    /// <summary>
    /// 用消息与内部异常构造
    /// </summary>
    /// <param name="message">错误信息</param>
    /// <param name="innerException">内部异常</param>
    public DomainException(string message, Exception innerException)
        : base(message: message, innerException: innerException)
    {
    }
}

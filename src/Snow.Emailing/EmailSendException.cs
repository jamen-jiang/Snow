namespace Snow.Emailing;

/// <summary>
/// 邮件发送失败: 统一包装底层 SMTP 异常, 业务层不必引用具体实现库
/// </summary>
public class EmailSendException : Exception
{
    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="message">错误说明</param>
    /// <param name="host">SMTP 服务器地址</param>
    /// <param name="recipients">涉及收件人</param>
    /// <param name="innerException">底层异常</param>
    public EmailSendException(
        string message,
        string? host = null,
        IEnumerable<string>? recipients = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        Host = host;
        Recipients = recipients?.ToArray() ?? [];
    }

    /// <summary>
    /// SMTP 服务器地址
    /// </summary>
    public string? Host { get; }

    /// <summary>
    /// 涉及收件人
    /// </summary>
    public IReadOnlyList<string> Recipients { get; }
}

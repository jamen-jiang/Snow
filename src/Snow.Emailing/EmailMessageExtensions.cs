using System.Linq;

namespace Snow.Emailing;

/// <summary>
/// <see cref="EmailMessage"/> 的链式语法糖: 调用点仍可像 FluentEmail 一样连着写,
/// 但状态都落在传入的消息对象上, 不存放在发送器里
/// </summary>
public static class EmailMessageExtensions
{
    /// <summary>
    /// 设置发件人
    /// </summary>
    public static EmailMessage From(this EmailMessage message, string address, string? displayName = null)
    {
        ArgumentNullException.ThrowIfNull(message);
        message.From = new EmailAddress(address, displayName);
        return message;
    }

    /// <summary>
    /// 追加收件人(支持逗号分隔多个)
    /// </summary>
    public static EmailMessage To(this EmailMessage message, string addresses)
    {
        ArgumentNullException.ThrowIfNull(message);
        message.To.AddRange(EmailAddress.ParseList(addresses));
        return message;
    }

    /// <summary>
    /// 追加抄送人(支持逗号分隔多个)
    /// </summary>
    public static EmailMessage Cc(this EmailMessage message, string addresses)
    {
        ArgumentNullException.ThrowIfNull(message);
        message.Cc.AddRange(EmailAddress.ParseList(addresses));
        return message;
    }

    /// <summary>
    /// 追加密送人(支持逗号分隔多个)
    /// </summary>
    public static EmailMessage Bcc(this EmailMessage message, string addresses)
    {
        ArgumentNullException.ThrowIfNull(message);
        message.Bcc.AddRange(EmailAddress.ParseList(addresses));
        return message;
    }

    /// <summary>
    /// 追加回复地址(支持逗号分隔多个)
    /// </summary>
    public static EmailMessage ReplyTo(this EmailMessage message, string addresses)
    {
        ArgumentNullException.ThrowIfNull(message);
        message.ReplyTo.AddRange(EmailAddress.ParseList(addresses));
        return message;
    }

    /// <summary>
    /// 设置标题
    /// </summary>
    public static EmailMessage Subject(this EmailMessage message, string subject)
    {
        ArgumentNullException.ThrowIfNull(message);
        message.Subject = subject;
        return message;
    }

    /// <summary>
    /// 设置 HTML 正文
    /// </summary>
    public static EmailMessage Html(this EmailMessage message, string html)
    {
        ArgumentNullException.ThrowIfNull(message);
        message.HtmlBody = html;
        return message;
    }

    /// <summary>
    /// 设置纯文本正文
    /// </summary>
    public static EmailMessage Text(this EmailMessage message, string text)
    {
        ArgumentNullException.ThrowIfNull(message);
        message.TextBody = text;
        return message;
    }

    /// <summary>
    /// 设置正文; 按 <see cref="EmailOptions.IsBodyHtml"/> 落到 HTML 或纯文本
    /// </summary>
    /// <param name="message">邮件</param>
    /// <param name="content">正文</param>
    /// <param name="isBodyHtml">是否按 HTML 处理; 默认 true</param>
    public static EmailMessage Content(this EmailMessage message, string content, bool isBodyHtml = true)
    {
        ArgumentNullException.ThrowIfNull(message);
        if (isBodyHtml)
        {
            message.HtmlBody = content;
        }
        else
        {
            message.TextBody = content;
        }

        return message;
    }

    /// <summary>
    /// 追加内存附件
    /// </summary>
    public static EmailMessage Attach(this EmailMessage message, string fileName, byte[] data, string? contentType = null)
    {
        ArgumentNullException.ThrowIfNull(message);
        message.Attachments.Add(new EmailAttachment(fileName, data, contentType));
        return message;
    }

    /// <summary>
    /// 追加惰性流附件
    /// </summary>
    public static EmailMessage Attach(this EmailMessage message, string fileName, Func<Stream> openStream, string? contentType = null)
    {
        ArgumentNullException.ThrowIfNull(message);
        message.Attachments.Add(new EmailAttachment(fileName, openStream, contentType));
        return message;
    }

    /// <summary>
    /// 追加已打开的流作为附件(投递时读取)
    /// </summary>
    public static EmailMessage Attach(this EmailMessage message, string fileName, Stream stream, string? contentType = null)
    {
        ArgumentNullException.ThrowIfNull(stream);
        return message.Attach(fileName, () => stream, contentType);
    }

    /// <summary>
    /// 追加自定义邮件头
    /// </summary>
    public static EmailMessage Header(this EmailMessage message, string name, string value)
    {
        ArgumentNullException.ThrowIfNull(message);
        message.Headers[name] = value;
        return message;
    }

    /// <summary>
    /// 设置优先级
    /// </summary>
    public static EmailMessage Priority(this EmailMessage message, EmailPriority priority)
    {
        ArgumentNullException.ThrowIfNull(message);
        message.Priority = priority;
        return message;
    }

    /// <summary>
    /// 设置单封超时
    /// </summary>
    public static EmailMessage Timeout(this EmailMessage message, TimeSpan timeout)
    {
        ArgumentNullException.ThrowIfNull(message);
        message.Timeout = timeout;
        return message;
    }
}

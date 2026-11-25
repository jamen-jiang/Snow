namespace Snow.Emailing;

/// <summary>
/// 一封邮件: 只装数据, 不含发送逻辑
/// <para>每封信一个独立实例, 发送后状态不会串到下一封(区别于把状态存在发送器字段上的写法)。</para>
/// </summary>
public sealed class EmailMessage
{
    /// <summary>
    /// 发件人; 为空时用配置项 <see cref="EmailSettingNames.DefaultFromAddress"/> 与 <see cref="EmailSettingNames.DefaultFromDisplayName"/>
    /// </summary>
    public EmailAddress? From { get; set; }

    /// <summary>
    /// 收件人
    /// </summary>
    public List<EmailAddress> To { get; } = [];

    /// <summary>
    /// 抄送
    /// </summary>
    public List<EmailAddress> Cc { get; } = [];

    /// <summary>
    /// 密送
    /// </summary>
    public List<EmailAddress> Bcc { get; } = [];

    /// <summary>
    /// 回复地址
    /// </summary>
    public List<EmailAddress> ReplyTo { get; } = [];

    /// <summary>
    /// 标题
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// 纯文本正文
    /// </summary>
    public string? TextBody { get; set; }

    /// <summary>
    /// HTML 正文; 与 <see cref="TextBody"/> 同时给则按 multipart/alternative 投递
    /// </summary>
    public string? HtmlBody { get; set; }

    /// <summary>
    /// 附件
    /// </summary>
    public List<EmailAttachment> Attachments { get; } = [];

    /// <summary>
    /// 自定义邮件头(键不区分大小写)
    /// </summary>
    public Dictionary<string, string> Headers { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 优先级
    /// </summary>
    public EmailPriority Priority { get; set; } = EmailPriority.Normal;

    /// <summary>
    /// 单封超时; 为空则用 <see cref="EmailOptions.Timeout"/>
    /// </summary>
    public TimeSpan? Timeout { get; set; }
}

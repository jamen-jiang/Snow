using Microsoft.Extensions.Logging;
using Snow.DependencyInjection;

namespace Snow.Emailing;

/// <summary>
/// 静默邮件发送器: 不连任何服务器, 只把要发的内容记进日志
/// <para>作为 <see cref="IEmailSender"/> 的兜底实现, 引入 Snow.Emailing.MailKit 后由
/// <c>MailKitEmailSender</c> 顶替。这样开发期不配 SMTP 也能启动, 生产漏配则只在日志里留痕。</para>
/// </summary>
public sealed class NullEmailSender : IEmailSender, ITransientDependency
{
    private readonly ILogger<NullEmailSender>? _logger;

    private readonly List<string> _to = [];
    private readonly List<string> _cc = [];
    private readonly List<string> _attachments = [];
    private string? _subject;
    private string? _content;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="logger">日志</param>
    public NullEmailSender(ILogger<NullEmailSender>? logger = null)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public IEmailSender Attach(string fileName, byte[] data)
    {
        _attachments.Add(fileName);
        return this;
    }

    /// <inheritdoc />
    public IEmailSender To(string to)
    {
        _to.Add(to);
        return this;
    }

    /// <inheritdoc />
    public IEmailSender Cc(string cc)
    {
        _cc.Add(cc);
        return this;
    }

    /// <inheritdoc />
    public IEmailSender Subject(string subject)
    {
        _subject = subject;
        return this;
    }

    /// <inheritdoc />
    public IEmailSender Content(string content)
    {
        _content = content;
        return this;
    }

    /// <inheritdoc />
    public Task SendAsync()
    {
        _logger?.LogDebug(
            "邮件未发送(当前为静默实现): 收件人 {To}, 抄送 {Cc}, 标题 {Subject}, 内容长度 {ContentLength}, 附件 {Attachments}",
            string.Join(", ", _to), string.Join(", ", _cc), _subject, _content?.Length ?? 0, string.Join(", ", _attachments));

        return Task.CompletedTask;
    }
}

using Microsoft.Extensions.Logging;
using Snow.DependencyInjection;

namespace Snow.Emailing;

/// <summary>
/// 静默邮件发送器: 不连任何服务器, 只把要发的内容记进日志
/// <para>作为 <see cref="IEmailSender"/> 的兜底实现, 引入 Snow.Emailing.MailKit 后由
/// <c>MailKitEmailSender</c> 顶替。这样开发期不配 SMTP 也能启动, 生产漏配则只在日志里留痕。</para>
/// </summary>
[TransientService]
public sealed class NullEmailSender : IEmailSender
{
    private readonly ILogger<NullEmailSender>? _logger;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="logger">日志</param>
    public NullEmailSender(ILogger<NullEmailSender>? logger = null)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        _logger?.LogDebug(
            "邮件未发送(当前为静默实现): 标题 {Subject}, 收件人 {To}, 抄送 {Cc}, 密送 {Bcc}, 附件 {AttachmentCount} 个",
            message.Subject,
            Join(message.To),
            Join(message.Cc),
            Join(message.Bcc),
            message.Attachments.Count);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SendAsync(Action<EmailMessage> configure, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configure);

        EmailMessage message = new();
        configure(message);
        return SendAsync(message, cancellationToken);
    }

    /// <inheritdoc />
    public async Task SendManyAsync(IEnumerable<EmailMessage> messages, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messages);

        foreach (EmailMessage message in messages)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await SendAsync(message, cancellationToken).ConfigureAwait(false);
        }
    }

    private static string Join(List<EmailAddress> addresses) => string.Join(", ", addresses);
}

// 扩展方法 SendAsync(MimeMessage, ...) 在 MailKit 命名空间; 本文件所在命名空间以 .MailKit 结尾,
// 裸写 using MailKit 会被解析成自己, 必须加 global::
using global::MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Utils;
using Snow.DependencyInjection;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;

namespace Snow.Emailing.MailKit;

/// <summary>
/// 基于 MailKit 的邮件发送器: MimeKit 组装正文与附件, MailKit 的 <see cref="SmtpClient"/> 走 SSL/STARTTLS 投递
/// <para>顶替 <see cref="NullEmailSender"/>: 引入本包后注入到的 <see cref="IEmailSender"/> 就是它。</para>
/// <para>无状态: 要发的数据都在传入的 <see cref="EmailMessage"/> 上, 同一个实例可以反复给不同的信投递。</para>
/// <para>连接加密方式只由 <see cref="EmailOptions.UseSsl"/> 决定: true 走 465 直连 SSL, false 走 STARTTLS。</para>
/// </summary>
[TransientService(ReplaceServices = true)]
public sealed class MailKitEmailSender : IEmailSender, IMailKitEmailSender
{
    private readonly EmailOptions _options;
    private readonly ILogger<MailKitEmailSender>? _logger;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="options">邮件配置</param>
    /// <param name="logger">日志</param>
    public MailKitEmailSender(
        IOptions<EmailOptions> options,
        ILogger<MailKitEmailSender>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        return SendManyAsync(new[] { message }, cancellationToken);
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

        EmailMessage[] list = messages.ToArray();
        if (list.Length == 0)
        {
            return;
        }

        // 建连与认证只做一次, 整批复用同一条连接
        using CancellationTokenSource? connectTimeout = CreateTimeoutSource(_options.Timeout, cancellationToken);
        CancellationToken connectToken = connectTimeout?.Token ?? cancellationToken;

        using SmtpClient client = await BuildClientAsync(connectToken).ConfigureAwait(false);

        foreach (EmailMessage message in list)
        {
            MimeMessage mimeMessage = BuildMessage(message);

            using CancellationTokenSource? sendTimeout = CreateTimeoutSource(message.Timeout ?? _options.Timeout, cancellationToken);
            CancellationToken sendToken = sendTimeout?.Token ?? cancellationToken;

            try
            {
                await client.SendAsync(mimeMessage, sendToken).ConfigureAwait(false);

                _logger?.LogDebug(
                    "发送邮件: 收件人 {To}, 抄送 {Cc}, 标题 {Subject}, 附件 {AttachmentCount} 个",
                    JoinAddresses(message.To), JoinAddresses(message.Cc), mimeMessage.Subject, message.Attachments.Count);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw new EmailSendException(
                    "发送邮件超时(" + FormatTimeout(message.Timeout ?? _options.Timeout) + ")",
                    _options.SmtpHost,
                    ToRecipientStrings(message));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                throw WrapException(ex, message);
            }
        }

        await client.DisconnectAsync(quit: true, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<SmtpClient> BuildClientAsync()
    {
        return BuildClientAsync(CancellationToken.None);
    }

    /// <summary>
    /// 建立并完成认证的 SMTP 连接
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>已连接的客户端, 调用方负责释放</returns>
    public async Task<SmtpClient> BuildClientAsync(CancellationToken cancellationToken)
    {
        SmtpClient client = new();

        try
        {
            await client.ConnectAsync(_options.SmtpHost, _options.Port, ResolveSecureSocketOption(), cancellationToken).ConfigureAwait(false);

            // 匿名中继(内网 SMTP、开发期的 MailHog 之类)不需要账号, 配了才认证
            if (!string.IsNullOrEmpty(_options.UserName))
            {
                await client.AuthenticateAsync(_options.UserName, _options.Password, cancellationToken).ConfigureAwait(false);
            }

            return client;
        }
        catch
        {
            client.Dispose();
            throw;
        }
    }

    private MimeMessage BuildMessage(EmailMessage message)
    {
        MimeMessage mimeMessage = new();

        // 发件人: 优先信上指定的, 否则回退配置的默认发件人
        string? fromAddress = message.From?.Address ?? (string.IsNullOrWhiteSpace(_options.DefaultFromAddress) ? null : _options.DefaultFromAddress);
        if (fromAddress is not null)
        {
            string? displayName = message.From?.DisplayName
                ?? (string.IsNullOrWhiteSpace(_options.DefaultFromDisplayName) ? null : _options.DefaultFromDisplayName);

            mimeMessage.From.Add(new MailboxAddress(displayName ?? string.Empty, fromAddress));
        }

        mimeMessage.To.AddRange(ToMailboxAddresses(message.To));
        if (message.Cc.Count > 0)
        {
            mimeMessage.Cc.AddRange(ToMailboxAddresses(message.Cc));
        }

        if (message.Bcc.Count > 0)
        {
            mimeMessage.Bcc.AddRange(ToMailboxAddresses(message.Bcc));
        }

        if (message.ReplyTo.Count > 0)
        {
            mimeMessage.ReplyTo.AddRange(ToMailboxAddresses(message.ReplyTo));
        }

        mimeMessage.Subject = message.Subject ?? string.Empty;
        mimeMessage.MessageId = MimeUtils.GenerateMessageId();
        mimeMessage.Priority = message.Priority switch
        {
            EmailPriority.High => MessagePriority.Urgent,
            EmailPriority.Low => MessagePriority.NonUrgent,
            _ => MessagePriority.Normal
        };

        foreach (KeyValuePair<string, string> header in message.Headers)
        {
            mimeMessage.Headers.Add(header.Key, header.Value);
        }

        BodyBuilder body = new();
        if (!string.IsNullOrEmpty(message.HtmlBody))
        {
            body.HtmlBody = message.HtmlBody;
        }

        if (!string.IsNullOrEmpty(message.TextBody))
        {
            body.TextBody = message.TextBody;
        }

        if (body.HtmlBody is null && body.TextBody is null)
        {
            // 两种正文都没给: 按配置给个空体, 保证 MimeMessage 有合法正文
            if (_options.IsBodyHtml)
            {
                body.HtmlBody = string.Empty;
            }
            else
            {
                body.TextBody = string.Empty;
            }
        }

        foreach (EmailAttachment attachment in message.Attachments)
        {
            body.Attachments.Add(attachment.FileName, attachment.OpenRead());
        }

        mimeMessage.Body = body.ToMessageBody();
        return mimeMessage;
    }

    private SecureSocketOptions ResolveSecureSocketOption()
    {
        // 465 端口直连即加密; 587/25 先明文开局再按服务器能力升级, 不支持 STARTTLS 就退回明文(内网中继常见)
        return _options.UseSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTlsWhenAvailable;
    }

    private EmailSendException WrapException(Exception exception, EmailMessage message)
    {
        string reason = exception switch
        {
            SmtpCommandException smtpCommand when smtpCommand.ErrorCode == SmtpErrorCode.RecipientNotAccepted =>
                "收件人被服务器拒绝: " + smtpCommand.Message,
            SmtpCommandException smtpCommand =>
                "SMTP 服务器拒绝(" + smtpCommand.StatusCode + " " + smtpCommand.ErrorCode + "): " + smtpCommand.Message,
            SmtpProtocolException protocol =>
                "SMTP 协议错误: " + protocol.Message,
            AuthenticationException authentication =>
                "SMTP 认证失败: " + authentication.Message,
            _ => "发送邮件失败: " + exception.Message
        };

        _logger?.LogError(exception, "发送邮件失败: 收件人 {To}, 标题 {Subject}", JoinAddresses(message.To), message.Subject);

        return new EmailSendException(reason, _options.SmtpHost, ToRecipientStrings(message), exception);
    }

    private static CancellationTokenSource? CreateTimeoutSource(TimeSpan? timeout, CancellationToken cancellationToken)
    {
        if (timeout is not { } value || value <= TimeSpan.Zero)
        {
            return null;
        }

        CancellationTokenSource source = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        source.CancelAfter(value);
        return source;
    }

    private static IEnumerable<MailboxAddress> ToMailboxAddresses(IEnumerable<EmailAddress> addresses)
    {
        return addresses.Select(address => new MailboxAddress(address.DisplayName ?? string.Empty, address.Address));
    }

    private static string JoinAddresses(IEnumerable<EmailAddress> addresses) => string.Join(", ", addresses);

    private static string[] ToRecipientStrings(EmailMessage message)
    {
        return message.To.Select(address => address.Address).ToArray();
    }

    private static string FormatTimeout(TimeSpan? timeout)
    {
        return timeout is { } value ? value.TotalSeconds.ToString("0.###") + " 秒" : "不限时";
    }
}

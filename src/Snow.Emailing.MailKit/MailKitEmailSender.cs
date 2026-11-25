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
using Snow.Emailing.Configuration;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;

namespace Snow.Emailing.MailKit;

/// <summary>
/// 基于 MailKit 的邮件发送器: MimeKit 组装正文与附件, MailKit 的 <see cref="SmtpClient"/> 走 SSL/STARTTLS 投递
/// <para>顶替 <see cref="NullEmailSender"/>: 引入本包后注入到的 <see cref="IEmailSender"/> 就是它。</para>
/// <para>SMTP 地址/端口/账号/密码/发件人都来自配置项系统(见 <see cref="EmailSettingNames"/>),
/// 与 ABP 一样每次发信前现读, 所以改配置不用重启。</para>
/// <para>连接加密方式由 <c>Snow.Mailing.Smtp.EnableSsl</c> 决定: true 走 465 直连 SSL, false 走 STARTTLS。</para>
/// </summary>
[TransientService(ReplaceServices = true)]
public sealed class MailKitEmailSender : IEmailSender, IMailKitEmailSender
{
    private readonly ISmtpEmailSenderConfiguration _smtpConfiguration;
    private readonly EmailOptions _options;
    private readonly ILogger<MailKitEmailSender>? _logger;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="smtpConfiguration">SMTP 配置(来自配置项系统)</param>
    /// <param name="options">邮件通用开关</param>
    /// <param name="logger">日志</param>
    public MailKitEmailSender(
        ISmtpEmailSenderConfiguration smtpConfiguration,
        IOptions<EmailOptions> options,
        ILogger<MailKitEmailSender>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(smtpConfiguration);
        ArgumentNullException.ThrowIfNull(options);

        _smtpConfiguration = smtpConfiguration;
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

        SmtpSettings settings = await LoadSettingsAsync().ConfigureAwait(false);

        // 建连与认证只做一次, 整批复用同一条连接
        using CancellationTokenSource? connectTimeout = CreateTimeoutSource(_options.Timeout, cancellationToken);
        CancellationToken connectToken = connectTimeout?.Token ?? cancellationToken;

        using SmtpClient client = await BuildClientAsync(settings, connectToken).ConfigureAwait(false);

        foreach (EmailMessage message in list)
        {
            MimeMessage mimeMessage = BuildMessage(message, settings);

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
                    settings.Host,
                    ToRecipientStrings(message));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                throw WrapException(ex, message, settings.Host);
            }
        }

        await client.DisconnectAsync(quit: true, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<SmtpClient> BuildClientAsync()
    {
        return await BuildClientAsync(CancellationToken.None).ConfigureAwait(false);
    }

    /// <summary>
    /// 建立并完成认证的 SMTP 连接
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>已连接的客户端, 调用方负责释放</returns>
    public async Task<SmtpClient> BuildClientAsync(CancellationToken cancellationToken)
    {
        SmtpSettings settings = await LoadSettingsAsync().ConfigureAwait(false);
        return await BuildClientAsync(settings, cancellationToken).ConfigureAwait(false);
    }

    private async Task<SmtpClient> BuildClientAsync(SmtpSettings settings, CancellationToken cancellationToken)
    {
        SmtpClient client = new();

        try
        {
            await client
                .ConnectAsync(settings.Host, settings.Port, ResolveSecureSocketOption(settings.EnableSsl), cancellationToken)
                .ConfigureAwait(false);

            // 匿名中继(内网 SMTP、开发期的 MailHog 之类)用默认凭据, 不认证
            if (!settings.UseDefaultCredentials)
            {
                await client.AuthenticateAsync(settings.UserName!, settings.Password!, cancellationToken).ConfigureAwait(false);
            }

            return client;
        }
        catch
        {
            client.Dispose();
            throw;
        }
    }

    private async Task<SmtpSettings> LoadSettingsAsync()
    {
        bool useDefaultCredentials = await _smtpConfiguration.GetUseDefaultCredentialsAsync().ConfigureAwait(false);

        string? userName = null;
        string? password = null;
        if (!useDefaultCredentials)
        {
            userName = await _smtpConfiguration.GetUserNameAsync().ConfigureAwait(false);
            password = await _smtpConfiguration.GetPasswordAsync().ConfigureAwait(false);
        }

        return new SmtpSettings(
            await _smtpConfiguration.GetHostAsync().ConfigureAwait(false),
            await _smtpConfiguration.GetPortAsync().ConfigureAwait(false),
            await _smtpConfiguration.GetEnableSslAsync().ConfigureAwait(false),
            useDefaultCredentials,
            userName,
            password,
            await _smtpConfiguration.GetDefaultFromAddressAsync().ConfigureAwait(false),
            await _smtpConfiguration.GetDefaultFromDisplayNameAsync().ConfigureAwait(false));
    }

    private MimeMessage BuildMessage(EmailMessage message, SmtpSettings settings)
    {
        MimeMessage mimeMessage = new();

        // 发件人: 优先信上指定的, 否则用配置项里的默认发件人
        string fromAddress = message.From?.Address ?? settings.DefaultFromAddress;
        string? displayName = message.From?.DisplayName ?? settings.DefaultFromDisplayName;
        mimeMessage.From.Add(new MailboxAddress(displayName ?? string.Empty, fromAddress));

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

        return mimeMessage;
    }

    private static SecureSocketOptions ResolveSecureSocketOption(bool enableSsl)
    {
        // 465 端口直连即加密; 587/25 先明文开局再按服务器能力升级, 不支持 STARTTLS 就退回明文(内网中继常见)
        return enableSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTlsWhenAvailable;
    }

    private EmailSendException WrapException(Exception exception, EmailMessage message, string host)
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

        return new EmailSendException(reason, host, ToRecipientStrings(message), exception);
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

    /// <summary>
    /// 一次发信所需的全部 SMTP 参数快照(整批共用, 避免每封信都读一遍配置项)
    /// </summary>
    private sealed record SmtpSettings(
        string Host,
        int Port,
        bool EnableSsl,
        bool UseDefaultCredentials,
        string? UserName,
        string? Password,
        string DefaultFromAddress,
        string DefaultFromDisplayName);
}

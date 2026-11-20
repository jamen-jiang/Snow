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
/// <para>一组链式调用对应一封信; 发送后<b>不清空</b>已设置的收件人与内容, 要再发一封请重新注入(Transient)。</para>
/// <para>连接加密方式只由 <see cref="EmailOptions.UseSsl"/> 决定: true 走 465 直连 SSL, false 走 STARTTLS。</para>
/// </summary>
[Dependency(ReplaceServices = true)]
public sealed class MailKitEmailSender : IEmailSender, IMailKitEmailSender, ITransientDependency
{
    private readonly EmailOptions _options;
    private readonly ILogger<MailKitEmailSender>? _logger;

    private readonly List<InternetAddress> _to = [];
    private readonly List<InternetAddress> _cc = [];
    private readonly List<(string FileName, byte[] Data)> _attachments = [];
    private string? _subject;
    private string? _content;

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
    public IEmailSender Attach(string fileName, byte[] data)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentNullException.ThrowIfNull(data);

        _attachments.Add((fileName, data));
        return this;
    }

    /// <inheritdoc />
    /// <remarks>支持 "a@x.com, b@y.com" 这种一次写多个地址, 也支持 "名字 &lt;a@x.com&gt;"</remarks>
    public IEmailSender To(string to)
    {
        _to.AddRange(ParseAddresses(to, nameof(to)));
        return this;
    }

    /// <inheritdoc />
    public IEmailSender Cc(string cc)
    {
        _cc.AddRange(ParseAddresses(cc, nameof(cc)));
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
    public async Task SendAsync()
    {
        MimeMessage message = BuildMessage();

        _logger?.LogDebug(
            "发送邮件: 收件人 {To}, 抄送 {Cc}, 标题 {Subject}, 附件 {AttachmentCount} 个",
            string.Join(", ", _to), string.Join(", ", _cc), message.Subject, _attachments.Count);

        CancellationTokenSource? timeoutSource = _options.Timeout is { } timeout && timeout > TimeSpan.Zero
            ? new CancellationTokenSource(timeout)
            : null;

        try
        {
            CancellationToken cancellationToken = timeoutSource?.Token ?? CancellationToken.None;

            using SmtpClient client = await BuildClientAsync(cancellationToken).ConfigureAwait(false);
            await client.SendAsync(message, cancellationToken).ConfigureAwait(false);
            await client.DisconnectAsync(quit: true, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "发送邮件失败: 收件人 {To}, 标题 {Subject}", string.Join(", ", _to), message.Subject);
            throw;
        }
        finally
        {
            timeoutSource?.Dispose();
        }
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
        if (string.IsNullOrWhiteSpace(_options.SmtpHost))
        {
            throw new InvalidOperationException("未配置 SMTP 服务器, 请设置 Email:SmtpHost。");
        }

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

    private MimeMessage BuildMessage()
    {
        if (string.IsNullOrWhiteSpace(_options.DefaultFromAddress))
        {
            throw new InvalidOperationException("未配置发件人地址, 请设置 Email:DefaultFromAddress。");
        }

        if (_to.Count == 0)
        {
            throw new InvalidOperationException("发送邮件前必须先指定收件人(调用 To)。");
        }

        MimeMessage message = new();
        message.From.Add(new MailboxAddress(_options.DefaultFromDisplayName, _options.DefaultFromAddress));
        message.To.AddRange(_to);
        if (_cc.Count > 0)
        {
            message.Cc.AddRange(_cc);
        }

        message.Subject = _subject ?? string.Empty;
        message.MessageId = MimeUtils.GenerateMessageId();

        BodyBuilder body = new();
        if (_options.IsBodyHtml)
        {
            body.HtmlBody = _content ?? string.Empty;
        }
        else
        {
            body.TextBody = _content ?? string.Empty;
        }

        foreach ((string fileName, byte[] data) in _attachments)
        {
            body.Attachments.Add(fileName, data);
        }

        message.Body = body.ToMessageBody();
        return message;
    }

    private SecureSocketOptions ResolveSecureSocketOption()
    {
        // 465 端口直连即加密; 587/25 先明文开局再按服务器能力升级, 不支持 STARTTLS 就退回明文(内网中继常见)
        return _options.UseSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTlsWhenAvailable;
    }

    private static List<InternetAddress> ParseAddresses(string value, string parameterName)
    {
        try
        {
            return InternetAddressList.Parse(value).Cast<InternetAddress>().ToList();
        }
        catch (ParseException ex)
        {
            throw new FormatException("邮件地址格式不正确: " + value, ex);
        }
    }
}

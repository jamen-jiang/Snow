using MailKit.Net.Smtp;

namespace Snow.Emailing.MailKit;

/// <summary>
/// 暴露底层 SMTP 连接的邮件发送器
/// <para>给需要自己控制连接(批量发信复用同一条连接、或调 MailKit 独有 API)的场景用;
/// 只发一封信直接用 <see cref="IEmailSender"/> 即可。</para>
/// </summary>
public interface IMailKitEmailSender : IEmailSender
{
    /// <summary>
    /// 建立并完成认证的 SMTP 连接
    /// </summary>
    /// <returns>已连接的客户端, <b>调用方负责释放</b></returns>
    Task<SmtpClient> BuildClientAsync();
}

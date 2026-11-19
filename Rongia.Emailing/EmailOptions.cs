namespace Rongia.Emailing;

/// <summary>
/// SMTP 邮件配置(对应 ABP 的 SmtpEmailSenderConfiguration + Settings 系统,
/// 这里简化为 IOptions 绑定 appsettings.json 的 "Email" 节)
/// </summary>
public class EmailOptions
{
    /// <summary>
    /// SMTP 服务器地址
    /// </summary>
    public string SmtpHost { get; set; } = string.Empty;

    /// <summary>
    /// SMTP 端口(SSL 直连 465,STARTTLS 587)
    /// </summary>
    public int Port { get; set; } = 465;

    /// <summary>
    /// 登录账号
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// 登录密码
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// 默认发件人地址
    /// </summary>
    public string DefaultFromAddress { get; set; } = string.Empty;

    /// <summary>
    /// 默认发件人显示名
    /// </summary>
    public string DefaultFromDisplayName { get; set; } = string.Empty;

    /// <summary>
    /// true = SSL 直连(465),false = STARTTLS(587)
    /// </summary>
    public bool UseSsl { get; set; } = true;
}

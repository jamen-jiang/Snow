namespace Snow.Emailing;

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
    /// true = 连接即 SSL(465),false = 走 STARTTLS(587): 服务器支持则升级加密, 不支持则明文
    /// </summary>
    public bool UseSsl { get; set; } = true;

    /// <summary>
    /// 单封邮件的整体超时(连接 + 认证 + 投递), 默认 100 秒;
    /// 设为 null 或非正数表示不限时
    /// <para>SMTP 服务器无响应时连接会一直挂着, 有超时才不会拖死调用方</para>
    /// </summary>
    public TimeSpan? Timeout { get; set; } = TimeSpan.FromSeconds(100);

    /// <summary>
    /// <see cref="IEmailSender.Content(string)"/> 传入的内容按 HTML 解析, 默认 true;
    /// 设为 false 则按纯文本投递
    /// </summary>
    public bool IsBodyHtml { get; set; } = true;
}

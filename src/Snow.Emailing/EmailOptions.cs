namespace Snow.Emailing;

/// <summary>
/// 邮件发送的通用开关(ABP 没有对应类型: SMTP 与发件人参数在配置项系统里, 见 <see cref="EmailSettingNames"/>)
/// </summary>
public class EmailOptions
{
    /// <summary>
    /// 单封邮件的整体超时(连接 + 认证 + 投递), 默认 100 秒;
    /// 设为 null 或非正数表示不限时
    /// <para>SMTP 服务器无响应时连接会一直挂着, 有超时才不会拖死调用方</para>
    /// </summary>
    public TimeSpan? Timeout { get; set; } = TimeSpan.FromSeconds(100);

    /// <summary>
    /// <see cref="EmailMessageExtensions.Content(EmailMessage, string, bool)"/> 未显式指定 isBodyHtml 时,
    /// 以及两种正文都没给时的空正文, 都按这个开关决定用 HTML 还是纯文本, 默认 true
    /// </summary>
    public bool IsBodyHtml { get; set; } = true;
}

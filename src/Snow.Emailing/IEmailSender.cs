namespace Snow.Emailing;

/// <summary>
/// 邮件发送器: 无状态, 一封邮件一个 <see cref="EmailMessage"/> 实例
/// <para>默认实现 <see cref="NullEmailSender"/> 只记日志不投递; 引入 Snow.Emailing.MailKit 后由
/// MailKitEmailSender 顶替。</para>
/// </summary>
public interface IEmailSender
{
    /// <summary>
    /// 发送一封邮件
    /// </summary>
    /// <param name="message">邮件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>表示发送操作的任务</returns>
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// 发送一封邮件(用回调就地组装)
    /// </summary>
    /// <param name="configure">组装回调</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>表示发送操作的任务</returns>
    Task SendAsync(Action<EmailMessage> configure, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量发送(同一连接内依次投递, 省去重复建连与认证)
    /// </summary>
    /// <param name="messages">邮件集合</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>表示发送操作的任务</returns>
    Task SendManyAsync(IEnumerable<EmailMessage> messages, CancellationToken cancellationToken = default);
}

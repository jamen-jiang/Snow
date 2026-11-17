namespace Rongia.Kernel.Application.Port.Email;

/// <summary>
/// 邮件发送器
/// </summary>
public interface IEmailSender
{
    /// <summary>
    /// 添加附件
    /// </summary>
    /// <param name="fileName">附件名</param>
    /// <param name="data">附件</param>
    IEmailSender Attach(string fileName, byte[] data);

    /// <summary>
    /// 设置收件人
    /// </summary>
    IEmailSender To(string to);

    /// <summary>
    /// 设置抄送人
    /// </summary>
    IEmailSender Cc(string cc);

    /// <summary>
    /// 设置标题
    /// </summary>
    IEmailSender Subject(string subject);

    /// <summary>
    /// 设置内容
    /// </summary>
    IEmailSender Content(string content);

    /// <summary>
    /// 发送
    /// </summary>
    void Send();
}

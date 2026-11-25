namespace Snow.Emailing.Configuration;

/// <summary>
/// SMTP 连接配置(对应 ABP 的 ISmtpEmailSenderConfiguration)
/// </summary>
public interface ISmtpEmailSenderConfiguration : IEmailSenderConfiguration
{
    /// <summary>
    /// 服务器地址
    /// </summary>
    /// <returns>地址; 未配置抛异常</returns>
    Task<string> GetHostAsync();

    /// <summary>
    /// 端口
    /// </summary>
    /// <returns>端口; 未配置或不是整数抛异常</returns>
    Task<int> GetPortAsync();

    /// <summary>
    /// 登录账号
    /// </summary>
    /// <returns>账号; 未配置抛异常</returns>
    Task<string> GetUserNameAsync();

    /// <summary>
    /// 登录密码(已由配置项系统解密)
    /// </summary>
    /// <returns>密码; 未配置抛异常</returns>
    Task<string> GetPasswordAsync();

    /// <summary>
    /// 认证域
    /// </summary>
    /// <returns>域; 未配置返回 null</returns>
    Task<string?> GetDomainAsync();

    /// <summary>
    /// 是否 SSL 直连
    /// </summary>
    /// <returns>true 走 465 直连 SSL, false 走 STARTTLS</returns>
    Task<bool> GetEnableSslAsync();

    /// <summary>
    /// 是否使用默认凭据
    /// </summary>
    /// <returns>true 表示匿名投递, 不做认证</returns>
    Task<bool> GetUseDefaultCredentialsAsync();
}

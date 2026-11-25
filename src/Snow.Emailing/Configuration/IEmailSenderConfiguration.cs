namespace Snow.Emailing.Configuration;

/// <summary>
/// 发件人配置(对应 ABP 的 IEmailSenderConfiguration)
/// </summary>
public interface IEmailSenderConfiguration
{
    /// <summary>
    /// 默认发件人地址
    /// </summary>
    /// <returns>地址; 未配置抛异常</returns>
    Task<string> GetDefaultFromAddressAsync();

    /// <summary>
    /// 默认发件人显示名
    /// </summary>
    /// <returns>显示名; 未配置抛异常</returns>
    Task<string> GetDefaultFromDisplayNameAsync();
}

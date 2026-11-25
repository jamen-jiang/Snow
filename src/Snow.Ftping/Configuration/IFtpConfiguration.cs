namespace Snow.Ftping.Configuration;

/// <summary>
/// FTP 连接配置: 全部值来自配置项系统(见 <see cref="FtpSettingNames"/>)
/// </summary>
public interface IFtpConfiguration
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
    /// <returns>账号; 未配置返回 null(按匿名登录处理)</returns>
    Task<string?> GetUserNameAsync();

    /// <summary>
    /// 登录密码(已由配置项系统解密)
    /// </summary>
    /// <returns>密码; 未配置返回 null</returns>
    Task<string?> GetPasswordAsync();

    /// <summary>
    /// 是否走 SSL/TLS 加密连接
    /// </summary>
    /// <returns>true 走显式 FTPS(AUTH TLS)</returns>
    Task<bool> GetUseSslAsync();

    /// <summary>
    /// 文件名编码
    /// </summary>
    /// <returns>编码名; 未配置时为空串</returns>
    Task<string> GetEncodingNameAsync();

    /// <summary>
    /// 连接超时(毫秒)
    /// </summary>
    /// <returns>毫秒数</returns>
    Task<int> GetConnectTimeoutAsync();

    /// <summary>
    /// 读写超时(毫秒)
    /// </summary>
    /// <returns>毫秒数</returns>
    Task<int> GetReadTimeoutAsync();

    /// <summary>
    /// 是否忽略证书校验
    /// </summary>
    /// <returns>true 表示不校验服务端证书</returns>
    Task<bool> GetIgnoreCertificateErrorsAsync();

    /// <summary>
    /// 失败重试次数
    /// </summary>
    /// <returns>次数</returns>
    Task<int> GetRetryAttemptsAsync();
}

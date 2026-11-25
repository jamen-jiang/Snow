namespace Snow.Ftping;

/// <summary>
/// FTP 相关配置项的名字, 全部以 <c>Snow.Ftp.</c> 开头
/// <para>值从 <c>Settings</c> 配置节读, 例如 <c>"Settings": { "Snow.Ftp.Host": "10.0.0.8" }</c>;
/// 也可以由其他取值来源(数据库等)提供。</para>
/// </summary>
public static class FtpSettingNames
{
    /// <summary>
    /// 服务器地址(必填)
    /// </summary>
    public const string Host = "Snow.Ftp.Host";

    /// <summary>
    /// 端口(默认 21, FTPS 隐式一般为 990)
    /// </summary>
    public const string Port = "Snow.Ftp.Port";

    /// <summary>
    /// 登录账号(留空按匿名登录处理, 用户名为 anonymous)
    /// </summary>
    public const string UserName = "Snow.Ftp.UserName";

    /// <summary>
    /// 登录密码(加密存储)
    /// </summary>
    public const string Password = "Snow.Ftp.Password";

    /// <summary>
    /// 是否走 SSL/TLS 加密连接
    /// </summary>
    public const string UseSsl = "Snow.Ftp.UseSsl";

    /// <summary>
    /// 文件名编码(中文乱码时改成 utf-8 或服务器实际编码, 如 gb2312)
    /// </summary>
    public const string EncodingName = "Snow.Ftp.EncodingName";

    /// <summary>
    /// 连接超时(毫秒)
    /// </summary>
    public const string ConnectTimeout = "Snow.Ftp.ConnectTimeout";

    /// <summary>
    /// 读写超时(毫秒)
    /// </summary>
    public const string ReadTimeout = "Snow.Ftp.ReadTimeout";

    /// <summary>
    /// 是否忽略证书校验(仅自签名证书的内网 FTP 设为 true)
    /// </summary>
    public const string IgnoreCertificateErrors = "Snow.Ftp.IgnoreCertificateErrors";

    /// <summary>
    /// 失败重试次数
    /// </summary>
    public const string RetryAttempts = "Snow.Ftp.RetryAttempts";
}

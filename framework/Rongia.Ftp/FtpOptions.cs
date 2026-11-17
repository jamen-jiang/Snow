namespace Rongia.Ftp;

/// <summary>
/// FTP 客户端配置(IOptions 绑定 appsettings.json 的 "Ftp" 节)
/// </summary>
public class FtpOptions
{
    /// <summary>
    /// FTP 服务器地址
    /// </summary>
    public string FtpHost { get; set; } = string.Empty;

    /// <summary>
    /// FTP 端口(默认 21,FTPS 隐式一般为 990)
    /// </summary>
    public int Port { get; set; } = 21;

    /// <summary>
    /// 登录账号
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// 登录密码
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// 是否走 SSL/TLS 加密连接
    /// </summary>
    public bool UseSsl { get; set; }

    /// <summary>
    /// 文件名编码(中文乱码时改成 utf-8 或服务器实际编码,如 gb2312)
    /// </summary>
    public string EncodingName { get; set; } = "utf-8";

    /// <summary>
    /// 连接超时(毫秒)
    /// </summary>
    public int ConnectTimeout { get; set; } = 10000;

    /// <summary>
    /// 读写超时(毫秒)
    /// </summary>
    public int ReadTimeout { get; set; } = 30000;

    /// <summary>
    /// 是否忽略证书校验(仅自签名证书的内网 FTP 设为 true)
    /// </summary>
    public bool IgnoreCertificateErrors { get; set; }

    /// <summary>
    /// 失败重试次数
    /// </summary>
    public int RetryAttempts { get; set; } = 3;
}

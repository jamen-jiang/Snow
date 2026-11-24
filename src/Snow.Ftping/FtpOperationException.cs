using System;

namespace Snow.Ftping;

/// <summary>
/// FTP 操作失败异常: 统一承载连接、鉴权、协议、传输等所有失败, 附主机与远端路径便于排查
/// </summary>
public class FtpOperationException : Exception
{
    /// <summary>
    /// FTP 服务器地址(可空)
    /// </summary>
    public string? Host { get; }

    /// <summary>
    /// 出错的远端路径(可空)
    /// </summary>
    public string? RemotePath { get; }

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="message">错误信息</param>
    /// <param name="host">FTP 服务器地址</param>
    /// <param name="remotePath">远端路径</param>
    /// <param name="innerException">内部异常</param>
    public FtpOperationException(string message, string? host = null, string? remotePath = null, Exception? innerException = null)
        : base(message, innerException)
    {
        Host = host;
        RemotePath = remotePath;
    }
}

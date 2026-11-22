using Microsoft.Extensions.Logging;
using Snow.DependencyInjection;

namespace Snow.Ftping;

/// <summary>
/// 静默 FTP 客户端: 不连任何服务器, 只把调用记进日志
/// <para>作为 <see cref="IFtpClient"/> 的兜底实现, 引入 Snow.Ftping.FluentFTP 后由
/// <c>FluentFtpClient</c> 顶替(它带 ReplaceServices)。这样开发期不配 FTP 也能启动。</para>
/// <para>与 <c>NullEmailSender</c> 的差别在返回值: 邮件的发送结果无处可返, 只能吞掉;
/// FTP 的上传/删除有 bool 与 void, 这里选择<b>如实报告失败</b>(上传返 false, 存在性判断返 false)
/// 而不是假装成功 —— 写入丢失是事故, 让调用方当场发现比事后排查便宜。</para>
/// </summary>
[TransientService]
public sealed class NullFtpClient : IFtpClient
{
    private readonly ILogger<NullFtpClient>? _logger;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="logger">日志</param>
    public NullFtpClient(ILogger<NullFtpClient>? logger = null)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public byte[]? Get(string path)
    {
        _logger?.LogDebug("FTP 读取未执行(当前为静默实现): 路径 {Path}", path);

        return null;
    }

    /// <inheritdoc />
    public bool Upload(Stream stream, string remotePath)
    {
        return ReportWrite("上传流", remotePath);
    }

    /// <inheritdoc />
    public bool Upload(byte[] buffer, string remotePath)
    {
        ArgumentNullException.ThrowIfNull(buffer);

        return ReportWrite("上传字节数组(" + buffer.Length + " 字节)", remotePath);
    }

    /// <inheritdoc />
    public bool Upload(string localPath, string remotePath)
    {
        return ReportWrite("上传本地文件 " + localPath, remotePath);
    }

    /// <inheritdoc />
    public void Remove(string path)
    {
        ReportWrite("删除", path);
    }

    /// <inheritdoc />
    public void CreateDirectory(string path)
    {
        ReportWrite("创建目录", path);
    }

    /// <inheritdoc />
    public bool FileExists(string path)
    {
        _logger?.LogDebug("FTP 存在性判断未执行(当前为静默实现): 路径 {Path}", path);

        return false;
    }

    /// <inheritdoc />
    public bool DirectoryExists(string path)
    {
        _logger?.LogDebug("FTP 目录判断未执行(当前为静默实现): 路径 {Path}", path);

        return false;
    }

    /// <inheritdoc />
    public IReadOnlyList<FtpFile> GetFtpFiles(string path)
    {
        _logger?.LogDebug("FTP 目录列举未执行(当前为静默实现): 路径 {Path}", path);

        return [];
    }

    /// <summary>
    /// 写操作统一留痕并报告失败
    /// </summary>
    /// <param name="operation">操作名</param>
    /// <param name="remotePath">目标路径</param>
    /// <returns>恒为 false</returns>
    private bool ReportWrite(string operation, string remotePath)
    {
        _logger?.LogWarning("FTP {Operation} 未执行(当前为静默实现, 需引入 Snow.Ftping.FluentFTP): 路径 {Path}", operation, remotePath);

        return false;
    }
}

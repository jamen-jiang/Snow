using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Snow.DependencyInjection;

namespace Snow.Ftping;

/// <summary>
/// 静默 FTP 客户端: 不连任何服务器。
/// <para>作为 <see cref="IFtpClient"/> 的兜底实现, 引入 Snow.Ftping.FluentFTP 后由 <c>FluentFtpClient</c>
/// (带 ReplaceServices)顶替, 因此开发期不配 FTP 也能正常启动。</para>
/// <para>与旧版"返回 null/false"不同, 本实现对每次调用<b>显式抛 <see cref="FtpOperationException"/></b>:
/// 这样"没装实现/没配置"与"文件真的不存在"不会混为一谈, 问题当场暴露而不是被静默吞掉。</para>
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
    /// <param name="path">路径</param>
    /// <param name="cancellationToken">取消令牌</param>
    public Task<byte[]?> ReadAllBytesAsync(string path, CancellationToken cancellationToken = default)
        => Fail<byte[]?>("读取文件", path);

    /// <inheritdoc />
    /// <param name="path">路径</param>
    /// <param name="cancellationToken">取消令牌</param>
    public Task<Stream> OpenReadAsync(string path, CancellationToken cancellationToken = default)
        => Fail<Stream>("打开读取流", path);

    /// <inheritdoc />
    /// <param name="remotePath">远程路径</param>
    /// <param name="destination">目标流</param>
    /// <param name="cancellationToken">取消令牌</param>
    public Task DownloadAsync(string remotePath, Stream destination, CancellationToken cancellationToken = default)
        => Fail("下载到流", remotePath);

    /// <inheritdoc />
    /// <param name="remotePath">远程路径</param>
    /// <param name="localPath">本地路径</param>
    /// <param name="overwrite">已存在时是否覆盖</param>
    /// <param name="cancellationToken">取消令牌</param>
    public Task DownloadFileAsync(string remotePath, string localPath, bool overwrite = true, CancellationToken cancellationToken = default)
        => Fail("下载到本地文件", remotePath);

    /// <inheritdoc />
    /// <param name="source">源对象</param>
    /// <param name="remotePath">远程路径</param>
    /// <param name="overwrite">已存在时是否覆盖</param>
    /// <param name="cancellationToken">取消令牌</param>
    public Task UploadAsync(Stream source, string remotePath, bool overwrite = true, CancellationToken cancellationToken = default)
        => Fail("上传流", remotePath);

    /// <inheritdoc />
    /// <param name="localPath">本地路径</param>
    /// <param name="remotePath">远程路径</param>
    /// <param name="overwrite">已存在时是否覆盖</param>
    /// <param name="cancellationToken">取消令牌</param>
    public Task UploadFileAsync(string localPath, string remotePath, bool overwrite = true, CancellationToken cancellationToken = default)
        => Fail("上传本地文件", remotePath);

    /// <inheritdoc />
    /// <param name="path">路径</param>
    /// <param name="cancellationToken">取消令牌</param>
    public Task DeleteFileAsync(string path, CancellationToken cancellationToken = default)
        => Fail("删除文件", path);

    /// <inheritdoc />
    /// <param name="path">路径</param>
    /// <param name="recursive">是否递归</param>
    /// <param name="cancellationToken">取消令牌</param>
    public Task DeleteDirectoryAsync(string path, bool recursive = false, CancellationToken cancellationToken = default)
        => Fail("删除目录", path);

    /// <inheritdoc />
    /// <param name="path">路径</param>
    /// <param name="cancellationToken">取消令牌</param>
    public Task CreateDirectoryAsync(string path, CancellationToken cancellationToken = default)
        => Fail("创建目录", path);

    /// <inheritdoc />
    /// <param name="sourcePath">源路径</param>
    /// <param name="destinationPath">目标路径</param>
    /// <param name="overwrite">已存在时是否覆盖</param>
    /// <param name="cancellationToken">取消令牌</param>
    public Task MoveFileAsync(string sourcePath, string destinationPath, bool overwrite = true, CancellationToken cancellationToken = default)
        => Fail("移动文件", sourcePath);

    /// <inheritdoc />
    /// <param name="path">路径</param>
    /// <param name="newName">新名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    public Task RenameAsync(string path, string newName, CancellationToken cancellationToken = default)
        => Fail("重命名文件", path);

    /// <inheritdoc />
    /// <param name="path">路径</param>
    /// <param name="cancellationToken">取消令牌</param>
    public Task<bool> FileExistsAsync(string path, CancellationToken cancellationToken = default)
        => Fail<bool>("判断文件是否存在", path);

    /// <inheritdoc />
    /// <param name="path">路径</param>
    /// <param name="cancellationToken">取消令牌</param>
    public Task<bool> DirectoryExistsAsync(string path, CancellationToken cancellationToken = default)
        => Fail<bool>("判断目录是否存在", path);

    /// <inheritdoc />
    /// <param name="path">路径</param>
    /// <param name="cancellationToken">取消令牌</param>
    public Task<FtpFileInfo?> GetFileInfoAsync(string path, CancellationToken cancellationToken = default)
        => Fail<FtpFileInfo?>("读取文件信息", path);

    /// <inheritdoc />
    /// <param name="path">路径</param>
    /// <param name="searchPattern">搜索模式, 可为 null</param>
    /// <param name="cancellationToken">取消令牌</param>
    public Task<IReadOnlyList<FtpEntry>> ListAsync(string path, string? searchPattern = null, CancellationToken cancellationToken = default)
        => Fail<IReadOnlyList<FtpEntry>>("列举目录", path);

    private Task Fail(string operation, string path)
    {
        _logger?.LogWarning("FTP {Operation} 未执行(当前为静默实现, 需引入 Snow.Ftping.FluentFTP): 路径 {Path}", operation, path);

        return Task.FromException(new FtpOperationException(
            "FTP " + operation + "未执行: 当前为静默实现, 请引入 Snow.Ftping.FluentFTP 包。路径: " + path,
            remotePath: path));
    }

    private Task<T> Fail<T>(string operation, string path)
    {
        _logger?.LogWarning("FTP {Operation} 未执行(当前为静默实现, 需引入 Snow.Ftping.FluentFTP): 路径 {Path}", operation, path);

        return Task.FromException<T>(new FtpOperationException(
            "FTP " + operation + "未执行: 当前为静默实现, 请引入 Snow.Ftping.FluentFTP 包。路径: " + path,
            remotePath: path));
    }
}

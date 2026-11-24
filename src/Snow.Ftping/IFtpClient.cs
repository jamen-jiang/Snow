using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Snow.Ftping;

/// <summary>
/// FTP 客户端接口(异步优先)。
/// <para>约定:</para>
/// <para>1. 所有方法都支持 <see cref="CancellationToken"/>, 网络往返不阻塞调用线程。</para>
/// <para>2. <b>失败即抛</b> <see cref="FtpOperationException"/>(带主机、远端路径与内部异常); <c>bool</c>/<c>null</c>
/// 只表达正常业务分支, 不承载"出错了"。</para>
/// <para>3. 一次方法调用 = 一条独立连接, 用完即断; 需要一次连接做多件事请用实现包的 IFluentFtpClient。</para>
/// </summary>
public interface IFtpClient
{
    /// <summary>
    /// 读取文件全部字节
    /// </summary>
    /// <param name="path">远端文件路径</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>文件内容; 文件不存在返回 null</returns>
    Task<byte[]?> ReadAllBytesAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// 打开文件读取流(适合大文件, 边读边处理)
    /// </summary>
    /// <param name="path">远端文件路径</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>只读流; 释放流会一并释放连接。文件不存在抛 <see cref="FtpOperationException"/></returns>
    Task<Stream> OpenReadAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// 下载文件并写入目标流
    /// </summary>
    /// <param name="remotePath">远端文件路径</param>
    /// <param name="destination">目标流(由调用方负责释放)</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task DownloadAsync(string remotePath, Stream destination, CancellationToken cancellationToken = default);

    /// <summary>
    /// 下载文件到本地路径
    /// </summary>
    /// <param name="remotePath">远端文件路径</param>
    /// <param name="localPath">本地保存路径</param>
    /// <param name="overwrite">本地已存在时是否覆盖</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task DownloadFileAsync(string remotePath, string localPath, bool overwrite = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// 上传流到远端路径(远端目录自动逐级创建)
    /// </summary>
    /// <param name="source">要上传的流(由调用方负责释放)</param>
    /// <param name="remotePath">远端目标路径</param>
    /// <param name="overwrite">远端已存在时是否覆盖; false 则跳过已有文件</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task UploadAsync(Stream source, string remotePath, bool overwrite = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// 上传本地文件到远端路径(远端目录自动逐级创建)
    /// </summary>
    /// <param name="localPath">本地文件路径</param>
    /// <param name="remotePath">远端目标路径</param>
    /// <param name="overwrite">远端已存在时是否覆盖; false 则跳过已有文件</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task UploadFileAsync(string localPath, string remotePath, bool overwrite = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除文件(文件本就不存在时视作成功, 幂等)
    /// </summary>
    /// <param name="path">远端文件路径</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task DeleteFileAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除目录(目录本就不存在时视作成功)
    /// </summary>
    /// <param name="path">远端目录路径</param>
    /// <param name="recursive">true 连同目录内内容一起删除; false 仅删空目录</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task DeleteDirectoryAsync(string path, bool recursive = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// 创建目录(逐级创建, 已存在不报错)
    /// </summary>
    /// <param name="path">远端目录路径</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task CreateDirectoryAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// 移动文件
    /// </summary>
    /// <param name="sourcePath">源路径</param>
    /// <param name="destinationPath">目标路径</param>
    /// <param name="overwrite">目标已存在时是否覆盖</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task MoveFileAsync(string sourcePath, string destinationPath, bool overwrite = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// 重命名文件(同一目录内改名)
    /// </summary>
    /// <param name="path">原远端文件路径</param>
    /// <param name="newName">新文件名(不含目录)</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task RenameAsync(string path, string newName, CancellationToken cancellationToken = default);

    /// <summary>
    /// 判断文件是否存在
    /// </summary>
    /// <param name="path">远端文件路径</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>存在为 true</returns>
    Task<bool> FileExistsAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// 判断目录是否存在
    /// </summary>
    /// <param name="path">远端目录路径</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>存在为 true</returns>
    Task<bool> DirectoryExistsAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// 取单个文件的元数据(不下载内容)
    /// </summary>
    /// <param name="path">远端文件路径</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>文件信息; 不存在返回 null</returns>
    Task<FtpFileInfo?> GetFileInfoAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// 列目录
    /// </summary>
    /// <param name="path">远端目录路径</param>
    /// <param name="searchPattern">可选文件名通配符(如 *.txt), 为空则返回全部</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>目录项(含文件、子目录、软链接)</returns>
    Task<IReadOnlyList<FtpEntry>> ListAsync(string path, string? searchPattern = null, CancellationToken cancellationToken = default);
}

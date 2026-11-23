using System.Net;
using System.Text;
using global::FluentFTP;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Snow.DependencyInjection;

namespace Snow.Ftping.FluentFTP;

/// <summary>
/// 基于 FluentFTP 的 FTP 客户端: 每次调用建一条连接, 用完即断
/// <para>顶替契约包的 <see cref="IFtpClient"/>: 引入本包后注入到的就是它。</para>
/// <para>所有方法都是同步的(与契约一致); 需要并发时由调用方各自注入(Transient), 本类不共享连接。</para>
/// <para>连接加密方式只由 <see cref="FtpOptions.UseSsl"/> 决定: true 走显式 FTPS(AUTH TLS), false 走明文 FTP。</para>
/// </summary>
[TransientService(ReplaceServices = true)]
public sealed class FluentFtpClient : IFtpClient, IFluentFtpClient
{
    private readonly FtpOptions _options;
    private readonly ILogger<FluentFtpClient>? _logger;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="options">FTP 配置</param>
    /// <param name="logger">日志</param>
    public FluentFtpClient(
        IOptions<FtpOptions> options,
        ILogger<FluentFtpClient>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public FtpClient BuildClient()
    {
        if (string.IsNullOrWhiteSpace(_options.FtpHost))
        {
            throw new InvalidOperationException("未配置 FTP 服务器地址: 请设置 Ftp:FtpHost。");
        }

        // 匿名的标准用户名是 anonymous: 留空时 FluentFTP 会直接拒绝空用户名
        string userName = string.IsNullOrWhiteSpace(_options.UserName) ? "anonymous" : _options.UserName;

        FtpClient client = new(
            _options.FtpHost,
            new NetworkCredential(userName, _options.Password),
            _options.Port,
            new FtpConfig())
        {
            Encoding = ResolveEncoding(),
        };

        client.Config.EncryptionMode = _options.UseSsl ? FtpEncryptionMode.Explicit : FtpEncryptionMode.None;
        client.Config.ConnectTimeout = _options.ConnectTimeout;
        client.Config.ReadTimeout = _options.ReadTimeout;
        client.Config.WriteTimeout = _options.ReadTimeout;
        client.Config.DataConnectionConnectTimeout = _options.ConnectTimeout;
        client.Config.DataConnectionReadTimeout = _options.ReadTimeout;
        client.Config.DataConnectionWriteTimeout = _options.ReadTimeout;
        client.Config.ValidateAnyCertificate = _options.IgnoreCertificateErrors;
        client.Config.RetryAttempts = _options.RetryAttempts;

        return client;
    }

    /// <inheritdoc />
    /// <remarks>先探一次存在性: 契约要求文件不存在时返回 null, 而不是抛异常</remarks>
    public byte[]? Get(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        return Execute(path, client =>
        {
            if (!client.FileExists(path))
            {
                return null;
            }

            return client.DownloadBytes(out byte[] bytes, path, 0, null, 0) ? bytes : null;
        });
    }

    /// <inheritdoc />
    /// <remarks>流不会被释放, 由调用方负责; 目标目录不存在时由 FluentFTP 逐级创建</remarks>
    public bool Upload(Stream stream, string remotePath)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentException.ThrowIfNullOrWhiteSpace(remotePath);

        return Execute(remotePath, client =>
        {
            FtpStatus status = client.UploadStream(stream, remotePath, FtpRemoteExists.Overwrite, true, null);
            return status == FtpStatus.Success;
        });
    }

    /// <inheritdoc />
    /// <remarks>目标目录不存在时由 FluentFTP 逐级创建</remarks>
    public bool Upload(byte[] buffer, string remotePath)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentException.ThrowIfNullOrWhiteSpace(remotePath);

        return Execute(remotePath, client =>
        {
            FtpStatus status = client.UploadBytes(buffer, remotePath, FtpRemoteExists.Overwrite, true, null);
            return status == FtpStatus.Success;
        });
    }

    /// <inheritdoc />
    /// <remarks>目标目录不存在时由 FluentFTP 逐级创建</remarks>
    public bool Upload(string localPath, string remotePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(localPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(remotePath);

        if (!File.Exists(localPath))
        {
            throw new FileNotFoundException("要上传的本地文件不存在: " + localPath, localPath);
        }

        return Execute(remotePath, client =>
        {
            FtpStatus status = client.UploadFile(localPath, remotePath, FtpRemoteExists.Overwrite, true, FtpVerify.None, null);
            return status == FtpStatus.Success;
        });
    }

    /// <inheritdoc />
    /// <remarks>文件本来就不存在时静默跳过(契约没有返回值, 不为此抛异常)</remarks>
    public void Remove(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        Execute(path, client =>
        {
            if (client.FileExists(path))
            {
                client.DeleteFile(path);
            }

            return true;
        });
    }

    /// <inheritdoc />
    /// <remarks>逐级创建, 目录已存在不报错</remarks>
    public void CreateDirectory(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        Execute(path, client => client.CreateDirectory(path, true));
    }

    /// <inheritdoc />
    public bool FileExists(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        return Execute(path, client => client.FileExists(path));
    }

    /// <inheritdoc />
    public bool DirectoryExists(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        return Execute(path, client => client.DirectoryExists(path));
    }

    /// <inheritdoc />
    public IReadOnlyList<FtpFile> GetFtpFiles(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        FtpListItem[] items = Execute(path, client => client.GetListing(path));

        List<FtpFile> files = [];
        foreach (FtpListItem item in items)
        {
            // 只要真文件: 目录、符号链接(LinkTarget 非空)、名字为空的脏条目一律剔除
            if (item.Type != FtpObjectType.File || item.LinkTarget is not null || string.IsNullOrEmpty(item.Name))
            {
                continue;
            }

            files.Add(new FtpFile
            {
                Name = item.Name,
                Path = string.IsNullOrEmpty(item.FullName) ? CombineRemotePath(path, item.Name) : item.FullName,
                Size = item.Size >= 0 ? item.Size : null,
                ModifiedTime = item.Modified == DateTime.MinValue ? null : item.Modified,
            });
        }

        return files;
    }

    /// <summary>
    /// 统一执行: 建连接 → 连接 → 执行 → 断开释放
    /// </summary>
    /// <typeparam name="T">返回值类型</typeparam>
    /// <param name="path">当前操作的远端路径(只用于错误日志)</param>
    /// <param name="action">要对已连接的客户端做的事</param>
    /// <returns>执行结果</returns>
    private T Execute<T>(string path, Func<FtpClient, T> action)
    {
        using FtpClient client = BuildClient();
        try
        {
            client.Connect();
            return action(client);
        }
        catch (Exception exception)
        {
            _logger?.LogError(exception, "FTP 操作失败: 远端路径为 {Path}, 服务器为 {Host}:{Port}", path, _options.FtpHost, _options.Port);
            throw;
        }
        finally
        {
            if (client.IsConnected)
            {
                client.Disconnect();
            }
        }
    }

    /// <summary>
    /// 拼接远端路径(父目录 + 文件名), 不重复斜杠
    /// </summary>
    /// <param name="directory">父目录</param>
    /// <param name="name">文件名</param>
    /// <returns>完整路径</returns>
    private static string CombineRemotePath(string directory, string name)
    {
        return directory.EndsWith('/') ? directory + name : directory + "/" + name;
    }

    /// <summary>
    /// 按配置解析文件名编码
    /// </summary>
    /// <returns>编码实例; 名字不认识时回退 utf-8</returns>
    private Encoding ResolveEncoding()
    {
        string name = string.IsNullOrWhiteSpace(_options.EncodingName) ? "utf-8" : _options.EncodingName;

        try
        {
            return Encoding.GetEncoding(name);
        }
        catch (ArgumentException)
        {
            _logger?.LogWarning("不支持的 FTP 文件名编码: {EncodingName}, 回退 utf-8", name);
            return Encoding.UTF8;
        }
    }
}

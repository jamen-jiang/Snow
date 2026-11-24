using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using global::FluentFTP;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Snow.DependencyInjection;

namespace Snow.Ftping.FluentFTP;

/// <summary>
/// 基于 FluentFTP 的异步 FTP 客户端: 每次调用建一条连接, 用完即断
/// <para>顶替契约包的 <see cref="IFtpClient"/>: 引入本包后注入到的就是它。</para>
/// <para>所有方法异步且支持取消; 需要一次连接做多件事时用 <see cref="IFluentFtpClient.BuildClient"/> 自管连接。</para>
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
    public AsyncFtpClient BuildClient()
    {
        if (string.IsNullOrWhiteSpace(_options.FtpHost))
        {
            throw new InvalidOperationException("未配置 FTP 服务器地址: 请设置 Ftp:FtpHost。");
        }

        // 匿名的标准用户名是 anonymous: 留空时 FluentFTP 会直接拒绝空用户名
        string userName = string.IsNullOrWhiteSpace(_options.UserName) ? "anonymous" : _options.UserName;

        AsyncFtpClient client = new(
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
    public Task<byte[]?> ReadAllBytesAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        return ExecuteAsync<byte[]?>(path, async (client, token) =>
        {
            if (!await client.FileExists(path, token).ConfigureAwait(false))
            {
                return null;
            }

            return await client.DownloadBytes(path, token).ConfigureAwait(false);
        }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Stream> OpenReadAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        AsyncFtpClient client = BuildClient();
        try
        {
            await client.Connect(cancellationToken).ConfigureAwait(false);
            Stream stream = await client.OpenRead(path, FtpDataType.Binary, 0, true, cancellationToken).ConfigureAwait(false);
            return new FtpOwnedStream(stream, client);
        }
        catch (Exception exception) when (exception is not FtpOperationException and not OperationCanceledException)
        {
            await client.DisposeAsync().ConfigureAwait(false);
            throw Wrap(path, exception);
        }
    }

    /// <inheritdoc />
    public Task DownloadAsync(string remotePath, Stream destination, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(remotePath);
        ArgumentNullException.ThrowIfNull(destination);

        return ExecuteAsync(remotePath, async (client, token) =>
        {
            if (!await client.FileExists(remotePath, token).ConfigureAwait(false))
            {
                throw new FtpOperationException("要下载的文件不存在: " + remotePath, _options.FtpHost, remotePath);
            }

            bool success = await client.DownloadStream(destination, remotePath, 0, null, token).ConfigureAwait(false);
            if (!success)
            {
                throw new FtpOperationException("下载失败: " + remotePath, _options.FtpHost, remotePath);
            }

            return true;
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task DownloadFileAsync(string remotePath, string localPath, bool overwrite = true, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(remotePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(localPath);

        return ExecuteAsync(remotePath, async (client, token) =>
        {
            FtpStatus status = await client
                .DownloadFile(localPath, remotePath, overwrite ? FtpLocalExists.Overwrite : FtpLocalExists.Skip, FtpVerify.None, null, token)
                .ConfigureAwait(false);

            if (status != FtpStatus.Success)
            {
                throw new FtpOperationException("下载失败(" + status + "): " + remotePath + " -> " + localPath, _options.FtpHost, remotePath);
            }

            return true;
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task UploadAsync(Stream source, string remotePath, bool overwrite = true, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentException.ThrowIfNullOrWhiteSpace(remotePath);

        return ExecuteAsync(remotePath, async (client, token) =>
        {
            FtpStatus status = await client
                .UploadStream(source, remotePath, ToRemoteExists(overwrite), true, null, token)
                .ConfigureAwait(false);

            if (status != FtpStatus.Success)
            {
                throw new FtpOperationException("上传失败(" + status + "): " + remotePath, _options.FtpHost, remotePath);
            }

            return true;
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task UploadFileAsync(string localPath, string remotePath, bool overwrite = true, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(localPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(remotePath);

        if (!File.Exists(localPath))
        {
            throw new FileNotFoundException("要上传的本地文件不存在: " + localPath, localPath);
        }

        return ExecuteAsync(remotePath, async (client, token) =>
        {
            FtpStatus status = await client
                .UploadFile(localPath, remotePath, ToRemoteExists(overwrite), true, FtpVerify.None, null, token)
                .ConfigureAwait(false);

            if (status != FtpStatus.Success)
            {
                throw new FtpOperationException("上传失败(" + status + "): " + localPath + " -> " + remotePath, _options.FtpHost, remotePath);
            }

            return true;
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task DeleteFileAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        return ExecuteAsync(path, async (client, token) =>
        {
            if (await client.FileExists(path, token).ConfigureAwait(false))
            {
                await client.DeleteFile(path, token).ConfigureAwait(false);
            }

            return true;
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task DeleteDirectoryAsync(string path, bool recursive = false, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        return ExecuteAsync(path, async (client, token) =>
        {
            if (!await client.DirectoryExists(path, token).ConfigureAwait(false))
            {
                return true;
            }

            if (recursive)
            {
                await client.DeleteDirectory(path, FtpListOption.Recursive, token).ConfigureAwait(false);
            }
            else
            {
                await client.DeleteDirectory(path, token).ConfigureAwait(false);
            }

            return true;
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task CreateDirectoryAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        return ExecuteAsync(path, (client, token) => client.CreateDirectory(path, true, token), cancellationToken);
    }

    /// <inheritdoc />
    public Task MoveFileAsync(string sourcePath, string destinationPath, bool overwrite = true, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);

        return ExecuteAsync(sourcePath, async (client, token) =>
        {
            bool moved = await client.MoveFile(sourcePath, destinationPath, ToRemoteExists(overwrite), token).ConfigureAwait(false);
            if (!moved)
            {
                throw new FtpOperationException("移动失败: " + sourcePath + " -> " + destinationPath, _options.FtpHost, sourcePath);
            }

            return true;
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task RenameAsync(string path, string newName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentException.ThrowIfNullOrWhiteSpace(newName);

        string destinationPath = CombineRemotePath(GetRemoteDirectory(path), newName);

        return ExecuteAsync(path, (client, token) => client.Rename(path, destinationPath, token), cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> FileExistsAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        return ExecuteAsync(path, (client, token) => client.FileExists(path, token), cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> DirectoryExistsAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        return ExecuteAsync(path, (client, token) => client.DirectoryExists(path, token), cancellationToken);
    }

    /// <inheritdoc />
    public Task<FtpFileInfo?> GetFileInfoAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        return ExecuteAsync<FtpFileInfo?>(path, async (client, token) =>
        {
            FtpListItem? item = await client.GetObjectInfo(path, true, token).ConfigureAwait(false);
            if (item is null || item.Type == FtpObjectType.Directory)
            {
                return null;
            }

            return new FtpFileInfo
            {
                Name = item.Name,
                Path = string.IsNullOrEmpty(item.FullName) ? path : item.FullName,
                Size = item.Size >= 0 ? item.Size : null,
                ModifiedTime = item.Modified == DateTime.MinValue ? null : item.Modified,
            };
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<FtpEntry>> ListAsync(string path, string? searchPattern = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        return ExecuteAsync<IReadOnlyList<FtpEntry>>(path, async (client, token) =>
        {
            FtpListItem[] items = await client.GetListing(path, token).ConfigureAwait(false);

            List<FtpEntry> entries = [];
            foreach (FtpListItem item in items)
            {
                if (string.IsNullOrEmpty(item.Name))
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(searchPattern) &&
                    !System.IO.Enumeration.FileSystemName.MatchesSimpleExpression(searchPattern, item.Name, ignoreCase: true))
                {
                    continue;
                }

                entries.Add(new FtpEntry
                {
                    Name = item.Name,
                    Path = string.IsNullOrEmpty(item.FullName) ? CombineRemotePath(path, item.Name) : item.FullName,
                    Type = ToEntryType(item.Type),
                    Size = item.Type == FtpObjectType.File && item.Size >= 0 ? item.Size : null,
                    ModifiedTime = item.Modified == DateTime.MinValue ? null : item.Modified,
                });
            }

            return entries;
        }, cancellationToken);
    }

    /// <summary>
    /// 统一执行: 建连接 → 连接 → 执行 → 断开释放
    /// </summary>
    private async Task<T> ExecuteAsync<T>(string path, Func<AsyncFtpClient, CancellationToken, Task<T>> action, CancellationToken cancellationToken)
    {
        AsyncFtpClient client = BuildClient();
        try
        {
            await client.Connect(cancellationToken).ConfigureAwait(false);
            return await action(client, cancellationToken).ConfigureAwait(false);
        }
        catch (FtpOperationException)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger?.LogError(exception, "FTP 操作失败: 远端路径为 {Path}, 服务器为 {Host}:{Port}", path, _options.FtpHost, _options.Port);
            throw Wrap(path, exception);
        }
        finally
        {
            try
            {
                if (client.IsConnected)
                {
                    await client.Disconnect(cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception exception)
            {
                _logger?.LogDebug(exception, "FTP 断开连接时出错(已忽略)");
            }

            await client.DisposeAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// 统一执行(无返回值)
    /// </summary>
    private Task ExecuteAsync(string path, Func<AsyncFtpClient, CancellationToken, Task> action, CancellationToken cancellationToken)
    {
        return ExecuteAsync(path, async (client, token) =>
        {
            await action(client, token).ConfigureAwait(false);
            return true;
        }, cancellationToken);
    }

    private FtpOperationException Wrap(string path, Exception exception)
    {
        return new FtpOperationException(
            "FTP 操作失败: " + path,
            _options.FtpHost,
            path,
            exception);
    }

    private static FtpRemoteExists ToRemoteExists(bool overwrite)
    {
        return overwrite ? FtpRemoteExists.Overwrite : FtpRemoteExists.Skip;
    }

    private static FtpEntryType ToEntryType(FtpObjectType type)
    {
        return type switch
        {
            FtpObjectType.Directory => FtpEntryType.Directory,
            FtpObjectType.Link => FtpEntryType.Link,
            _ => FtpEntryType.File,
        };
    }

    /// <summary>
    /// 取远端路径所在目录(不含末尾斜杠)
    /// </summary>
    private static string GetRemoteDirectory(string path)
    {
        int index = path.LastIndexOf('/');
        return index <= 0 ? "/" : path[..index];
    }

    /// <summary>
    /// 拼接远端路径(父目录 + 文件名), 不重复斜杠
    /// </summary>
    private static string CombineRemotePath(string directory, string name)
    {
        return directory.EndsWith('/') ? directory + name : directory + "/" + name;
    }

    /// <summary>
    /// 按配置解析文件名编码
    /// </summary>
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

    /// <summary>
    /// 把底层读取流和它依赖的连接绑在一起: 释放流时一并释放连接
    /// </summary>
    private sealed class FtpOwnedStream : Stream
    {
        private readonly Stream _inner;
        private readonly AsyncFtpClient _client;

        public FtpOwnedStream(Stream inner, AsyncFtpClient client)
        {
            _inner = inner;
            _client = client;
        }

        public override bool CanRead => _inner.CanRead;
        public override bool CanSeek => _inner.CanSeek;
        public override bool CanWrite => _inner.CanWrite;
        public override long Length => _inner.Length;

        public override long Position
        {
            get => _inner.Position;
            set => _inner.Position = value;
        }

        public override void Flush() => _inner.Flush();

        public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => _inner.ReadAsync(buffer, offset, count, cancellationToken);

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            => _inner.ReadAsync(buffer, cancellationToken);

        public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);

        public override void SetLength(long value) => _inner.SetLength(value);

        public override void Write(byte[] buffer, int offset, int count) => _inner.Write(buffer, offset, count);

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _inner.Dispose();
                _client.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}

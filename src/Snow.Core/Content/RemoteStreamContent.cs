using System.IO;

namespace Snow.Content;

/// <summary>
/// 远程流内容, 对应 ABP 的 <c>RemoteStreamContent</c>。
/// </summary>
public class RemoteStreamContent : IRemoteStreamContent
{
    private readonly Stream _stream;
    private readonly bool _disposeStream;
    private bool _disposed;

    /// <summary>
    /// 文件名。
    /// </summary>
    public virtual string? FileName { get; }

    /// <summary>
    /// 内容类型。
    /// </summary>
    public virtual string ContentType { get; } = "application/octet-stream";

    /// <summary>
    /// 内容长度, 未知时为 <c>null</c>。
    /// </summary>
    public virtual long? ContentLength { get; }

    /// <summary>
    /// 创建远程流内容。
    /// </summary>
    /// <param name="stream">内容流</param>
    /// <param name="fileName">文件名</param>
    /// <param name="contentType">内容类型</param>
    /// <param name="readOnlyLength">只读长度, 为 <c>null</c> 时按流自动计算</param>
    /// <param name="disposeStream">释放内容时是否释放底层流</param>
    public RemoteStreamContent(
        Stream stream,
        string? fileName = null,
        string? contentType = null,
        long? readOnlyLength = null,
        bool disposeStream = true)
    {
        _stream = stream;
        FileName = fileName;
        if (contentType != null)
        {
            ContentType = contentType;
        }

        ContentLength = readOnlyLength ?? (_stream.CanSeek ? _stream.Length - _stream.Position : null);
        _disposeStream = disposeStream;
    }

    /// <summary>
    /// 获取内容流。
    /// </summary>
    /// <returns>内容流</returns>
    public virtual Stream GetStream()
    {
        return _stream;
    }

    /// <summary>
    /// 释放内容。
    /// </summary>
    public virtual void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_disposeStream)
        {
            _stream.Dispose();
        }

        _disposed = true;
    }
}

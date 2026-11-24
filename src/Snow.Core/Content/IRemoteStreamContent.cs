using System;
using System.IO;

namespace Snow.Content;

/// <summary>
/// 远程流内容接口, 对应 ABP 的 <c>IRemoteStreamContent</c>。
/// </summary>
public interface IRemoteStreamContent : IDisposable
{
    /// <summary>
    /// 文件名。
    /// </summary>
    string? FileName { get; }

    /// <summary>
    /// 内容类型。
    /// </summary>
    string ContentType { get; }

    /// <summary>
    /// 内容长度, 未知时为 <c>null</c>。
    /// </summary>
    long? ContentLength { get; }

    /// <summary>
    /// 获取内容流。
    /// </summary>
    /// <returns>内容流</returns>
    Stream GetStream();
}

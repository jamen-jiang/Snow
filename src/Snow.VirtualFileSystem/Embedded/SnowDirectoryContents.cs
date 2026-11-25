using System.Collections;
using Microsoft.Extensions.FileProviders;

namespace Snow.VirtualFileSystem.Embedded;

/// <summary>
/// 目录内容
/// </summary>
internal sealed class SnowDirectoryContents : IDirectoryContents
{
    private readonly IReadOnlyList<IFileInfo> _files;

    public SnowDirectoryContents(IReadOnlyList<IFileInfo> files)
    {
        _files = files;
    }

    /// <inheritdoc />
    public bool Exists => _files.Count > 0;

    /// <inheritdoc />
    public IEnumerator<IFileInfo> GetEnumerator()
    {
        return _files.GetEnumerator();
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

using System.Reflection;
using Microsoft.Extensions.FileProviders;

namespace Snow.VirtualFileSystem.Embedded;

/// <summary>
/// 嵌入资源文件信息
/// </summary>
public sealed class SnowEmbeddedFileInfo : IFileInfo
{
    private readonly Assembly _assembly;
    private readonly string _resourceName;

    /// <summary>构造器</summary>
    public SnowEmbeddedFileInfo(Assembly assembly, string resourceName, string name, DateTimeOffset lastModified)
    {
        _assembly = assembly;
        _resourceName = resourceName;
        Name = name;
        LastModified = lastModified;
    }

    /// <inheritdoc />
    public bool Exists => true;

    /// <inheritdoc />
    public long Length
    {
        get
        {
            using Stream? stream = _assembly.GetManifestResourceStream(_resourceName);
            return stream?.Length ?? 0;
        }
    }

    /// <inheritdoc />
    public string PhysicalPath => string.Empty;

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public DateTimeOffset LastModified { get; }

    /// <inheritdoc />
    public bool IsDirectory => false;

    /// <inheritdoc />
    public Stream CreateReadStream()
    {
        return _assembly.GetManifestResourceStream(_resourceName)
            ?? throw new InvalidOperationException("找不到嵌入资源: " + _resourceName);
    }
}

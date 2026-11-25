using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Snow.VirtualFileSystem;

/// <summary>
/// 默认虚拟文件提供器(对应 ABP 的 <c>VirtualFileProvider</c>): 按注册顺序反向合成各文件集。
/// </summary>
public class SnowVirtualFileProvider : IVirtualFileProvider
{
    private readonly IFileProvider _hybridFileProvider;
    private readonly SnowVirtualFileSystemOptions _options;

    /// <summary>构造器</summary>
    public SnowVirtualFileProvider(IOptions<SnowVirtualFileSystemOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
        _hybridFileProvider = CreateHybridProvider();
    }

    /// <inheritdoc />
    public virtual IFileInfo GetFileInfo(string subpath)
    {
        return _hybridFileProvider.GetFileInfo(subpath);
    }

    /// <inheritdoc />
    public virtual IDirectoryContents GetDirectoryContents(string subpath)
    {
        if (subpath == string.Empty)
        {
            subpath = "/";
        }

        return _hybridFileProvider.GetDirectoryContents(subpath);
    }

    /// <inheritdoc />
    public virtual IChangeToken Watch(string filter)
    {
        return _hybridFileProvider.Watch(filter);
    }

    /// <summary>创建合成文件提供器</summary>
    protected virtual IFileProvider CreateHybridProvider()
    {
        var fileProviders = new List<IFileProvider>();
        foreach (VirtualFileSetInfo fileSet in _options.FileSets.AsEnumerable().Reverse())
        {
            fileProviders.Add(fileSet.FileProvider);
        }

        return new CompositeFileProvider(fileProviders);
    }
}

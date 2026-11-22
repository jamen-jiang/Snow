using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Snow.VirtualFileSystem;

/// <summary>
/// 虚拟文件提供器的默认实现: 首次访问时把登记的文件集合并成 <see cref="CompositeFileProvider"/>
/// <para>合并视图只构建一次(文件集在启动后不再变化), 之后每次读取都走同一个复合提供器。</para>
/// </summary>
public sealed class SnowVirtualFileProvider : IVirtualFileProvider
{
    private readonly IOptions<SnowVirtualFileSystemOptions> _options;
    private readonly Lazy<IFileProvider> _provider;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="options">虚拟文件系统选项</param>
    public SnowVirtualFileProvider(IOptions<SnowVirtualFileSystemOptions> options)
    {
        _options = options;
        _provider = new Lazy<IFileProvider>(BuildProvider);
    }

    /// <inheritdoc />
    public IFileInfo GetFileInfo(string? subpath) => _provider.Value.GetFileInfo(subpath ?? string.Empty);

    /// <inheritdoc />
    public IDirectoryContents GetDirectoryContents(string? subpath) => _provider.Value.GetDirectoryContents(subpath ?? string.Empty);

    /// <inheritdoc />
    public IChangeToken Watch(string filter) => _provider.Value.Watch(filter);

    private IFileProvider BuildProvider()
    {
        List<IFileProvider> providers = _options.Value.FileSets.Select(set => set.FileProvider).ToList();
        return providers.Count switch
        {
            0 => new NullFileProvider(),
            1 => providers[0],
            _ => new CompositeFileProvider(providers),
        };
    }

    private sealed class NullFileProvider : IFileProvider
    {
        public IFileInfo GetFileInfo(string? subpath) => new NotFoundFileInfo(subpath ?? string.Empty);

        public IDirectoryContents GetDirectoryContents(string? subpath) => NotFoundDirectoryContents.Singleton;

        public IChangeToken Watch(string filter) => NullChangeToken.Singleton;
    }
}

using Microsoft.Extensions.FileProviders;

namespace Snow.VirtualFileSystem;

/// <summary>
/// 虚拟文件系统选项: 登记要合并进统一视图的文件集
/// </summary>
public sealed class SnowVirtualFileSystemOptions
{
    /// <summary>
    /// 文件集列表, 按登记顺序合并(前面的优先)
    /// </summary>
    public VirtualFileSetList FileSets { get; } = new();

    /// <summary>
    /// 追加一个已有的文件提供器
    /// </summary>
    /// <param name="fileProvider">文件提供器</param>
    /// <param name="name">文件集名字</param>
    /// <returns>选项本身, 便于链式调用</returns>
    public SnowVirtualFileSystemOptions Add(IFileProvider fileProvider, string? name = null)
    {
        FileSets.Add(new VirtualFileSet(name, fileProvider));
        return this;
    }

    /// <summary>
    /// 追加一个物理目录
    /// </summary>
    /// <param name="path">绝对或相对路径</param>
    /// <param name="name">文件集名字</param>
    /// <returns>选项本身, 便于链式调用</returns>
    public SnowVirtualFileSystemOptions AddPhysical(string path, string? name = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return Add(new PhysicalFileProvider(Path.GetFullPath(path)), name ?? path);
    }

    /// <summary>
    /// 追加一个程序集内嵌资源目录
    /// </summary>
    /// <typeparam name="TMarker">程序集里的任意类型, 用来定位程序集</typeparam>
    /// <param name="rootPath">资源根路径, 如 "wwwroot"</param>
    /// <param name="name">文件集名字</param>
    /// <returns>选项本身, 便于链式调用</returns>
    public SnowVirtualFileSystemOptions AddEmbedded<TMarker>(string rootPath = "", string? name = null)
    {
        return Add(new SnowEmbeddedFileProvider(typeof(TMarker).Assembly, rootPath), name ?? typeof(TMarker).Assembly.GetName().Name);
    }
}

/// <summary>
/// 文件集集合
/// </summary>
public sealed class VirtualFileSetList : List<VirtualFileSet>
{
}

using Microsoft.Extensions.FileProviders;

namespace Snow.VirtualFileSystem;

/// <summary>
/// 一个虚拟文件集: 名字 + 一个文件提供器
/// <para>多个文件集按登记顺序合并, 越靠前优先级越高(同名文件以先登记者为准)。</para>
/// </summary>
public sealed class VirtualFileSet
{
    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="name">文件集名字, 仅用于诊断</param>
    /// <param name="fileProvider">文件提供器</param>
    public VirtualFileSet(string? name, IFileProvider fileProvider)
    {
        Name = name;
        FileProvider = fileProvider ?? throw new ArgumentNullException(nameof(fileProvider));
    }

    /// <summary>
    /// 文件集名字
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// 文件提供器
    /// </summary>
    public IFileProvider FileProvider { get; }
}

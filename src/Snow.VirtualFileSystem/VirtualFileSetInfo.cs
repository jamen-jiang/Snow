using Microsoft.Extensions.FileProviders;

namespace Snow.VirtualFileSystem;

/// <summary>
/// 虚拟文件集(对应 ABP 的 <c>VirtualFileSetInfo</c>)
/// </summary>
public class VirtualFileSetInfo
{
    /// <summary>文件提供器</summary>
    public IFileProvider FileProvider { get; }

    /// <summary>构造器</summary>
    public VirtualFileSetInfo(IFileProvider fileProvider)
    {
        FileProvider = Check.NotNull(fileProvider, nameof(fileProvider));
    }
}

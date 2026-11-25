namespace Snow.VirtualFileSystem;

/// <summary>
/// 虚拟文件系统选项(对应 ABP 的 <c>AbpVirtualFileSystemOptions</c>)
/// </summary>
public class SnowVirtualFileSystemOptions
{
    /// <summary>虚拟文件集</summary>
    public VirtualFileSetList FileSets { get; }

    /// <summary>构造器</summary>
    public SnowVirtualFileSystemOptions()
    {
        FileSets = new VirtualFileSetList();
    }
}

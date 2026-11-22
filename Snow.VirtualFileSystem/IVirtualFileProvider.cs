using Microsoft.Extensions.FileProviders;

namespace Snow.VirtualFileSystem;

/// <summary>
/// 虚拟文件提供器: 所有文件集合并后的统一只读视图(对应 ABP 的 IVirtualFileProvider)
/// </summary>
public interface IVirtualFileProvider : IFileProvider
{
}

using Microsoft.Extensions.FileProviders;

namespace Snow.VirtualFileSystem;

/// <summary>
/// 虚拟文件提供器(对应 ABP 的 <c>IVirtualFileProvider</c>): 本身没有新增成员, 方法全部来自 <see cref="IFileProvider"/>。
/// </summary>
public interface IVirtualFileProvider : IFileProvider
{
}

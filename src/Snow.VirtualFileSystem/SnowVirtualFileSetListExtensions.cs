using System.Reflection;
using Microsoft.Extensions.FileProviders;
using Snow.VirtualFileSystem.Embedded;

namespace Snow.VirtualFileSystem;

/// <summary>
/// 虚拟文件集扩展(对应 ABP 的 <c>VirtualFileSetListExtensions</c>)
/// </summary>
public static class SnowVirtualFileSetListExtensions
{
    /// <summary>添加程序集嵌入资源</summary>
    public static void AddEmbedded<T>(this VirtualFileSetList list, string? baseNamespace = null)
    {
        Check.NotNull(list, nameof(list));

        Assembly assembly = typeof(T).Assembly;
        list.Add(new VirtualFileSetInfo(new SnowEmbeddedFileProvider(assembly, baseNamespace)));
    }
}

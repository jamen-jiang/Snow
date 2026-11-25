using System.Reflection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace Snow.VirtualFileSystem.Embedded;

/// <summary>
/// 嵌入资源文件提供器(对应 ABP 的 <c>AbpEmbeddedFileProvider</c>): 把程序集的 ManifestResourceName 映射为虚拟路径。
/// <para>只收录文件条目, 不为目录造虚拟条目: 目录内容按"父路径匹配"现算, 与 ABP 里目录条目仅服务于其
/// DictionaryBasedFileProvider 的差异无关。</para>
/// </summary>
public class SnowEmbeddedFileProvider : IFileProvider
{
    private readonly Lazy<Dictionary<string, IFileInfo>> _files;

    /// <summary>程序集</summary>
    public Assembly Assembly { get; }

    /// <summary>基础命名空间</summary>
    public string? BaseNamespace { get; }

    /// <summary>构造器</summary>
    public SnowEmbeddedFileProvider(Assembly assembly, string? baseNamespace = null)
    {
        Assembly = Check.NotNull(assembly, nameof(assembly));
        BaseNamespace = baseNamespace;
        _files = new Lazy<Dictionary<string, IFileInfo>>(CreateFiles, isThreadSafe: true);
    }

    private Dictionary<string, IFileInfo> Files => _files.Value;

    /// <inheritdoc />
    public IFileInfo GetFileInfo(string subpath)
    {
        string path = NormalizePath(subpath);
        return Files.TryGetValue(path, out IFileInfo? file) ? file : new NotFoundFileInfo(subpath);
    }

    /// <inheritdoc />
    public IDirectoryContents GetDirectoryContents(string subpath)
    {
        string path = NormalizePath(subpath);

        var children = Files
            .Where(pair => GetParentPath(pair.Key) == path)
            .Select(pair => pair.Value)
            .OrderBy(child => child.Name, StringComparer.Ordinal)
            .ToList();

        return new SnowDirectoryContents(children);
    }

    /// <inheritdoc />
    public IChangeToken Watch(string filter)
    {
        // 嵌入资源在程序集加载后恒定不变, 与 ABP 一样永不触发变更
        return NullChangeToken.Singleton;
    }

    private Dictionary<string, IFileInfo> CreateFiles()
    {
        var files = new Dictionary<string, IFileInfo>(StringComparer.OrdinalIgnoreCase);
        DateTimeOffset lastModified = GetLastModificationTime();

        foreach (string resourceName in Assembly.GetManifestResourceNames())
        {
            if (!BaseNamespace.IsNullOrEmpty() &&
                !resourceName.StartsWith(BaseNamespace + ".", StringComparison.Ordinal))
            {
                continue;
            }

            string virtualPath = NormalizePath(ConvertToRelativePath(resourceName));
            files[virtualPath] = new SnowEmbeddedFileInfo(Assembly, resourceName, GetFileName(virtualPath), lastModified);
        }

        return files;
    }

    private string ConvertToRelativePath(string resourceName)
    {
        if (!BaseNamespace.IsNullOrEmpty())
        {
            resourceName = resourceName[(BaseNamespace.Length + 1)..];
        }

        string[] parts = resourceName.Split('.');
        if (parts.Length <= 2)
        {
            return resourceName;
        }

        return string.Join('/', parts.Take(parts.Length - 2)) + "/" + parts[^2] + "." + parts[^1];
    }

    private static string NormalizePath(string path)
    {
        if (path.IsNullOrEmpty())
        {
            return "/";
        }

        path = path.Replace('\\', '/');
        if (!path.StartsWith('/'))
        {
            path = "/" + path;
        }

        while (path.Contains("//", StringComparison.Ordinal))
        {
            path = path.Replace("//", "/", StringComparison.Ordinal);
        }

        if (path.Length > 1 && path.EndsWith('/'))
        {
            path = path.TrimEnd('/');
        }

        return path;
    }

    private static string GetParentPath(string path)
    {
        if (path == "/")
        {
            return "/";
        }

        int index = path.LastIndexOf('/');
        return index <= 0 ? "/" : path[..index];
    }

    private static string GetFileName(string path)
    {
        int index = path.LastIndexOf('/');
        return index < 0 ? path : path[(index + 1)..];
    }

    private DateTimeOffset GetLastModificationTime()
    {
        // 单文件发布等场景下程序集没有物理路径, 退回当前时间
        if (Assembly.Location.IsNullOrEmpty())
        {
            return DateTimeOffset.UtcNow;
        }

        try
        {
            return File.GetLastWriteTimeUtc(Assembly.Location);
        }
        catch (Exception)
        {
            return DateTimeOffset.UtcNow;
        }
    }
}

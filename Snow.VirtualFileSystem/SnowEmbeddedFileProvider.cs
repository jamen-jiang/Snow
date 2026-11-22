using System.Reflection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace Snow.VirtualFileSystem;

/// <summary>
/// 程序集内嵌资源文件提供器
/// <para>把 <c>{程序集名}.{根路径}.{子路径}</c> 形式的 manifest 资源当成文件树暴露,
/// 不引入额外 NuGet 包, 用于包内自带的 wwwroot / 模板 / 本地化资源。</para>
/// </summary>
public sealed class SnowEmbeddedFileProvider : IFileProvider
{
    private readonly Assembly _assembly;
    private readonly string _prefix;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="assembly">内嵌资源所在程序集</param>
    /// <param name="rootPath">资源根路径(相对程序集名的那一段), 可为空</param>
    public SnowEmbeddedFileProvider(Assembly assembly, string rootPath = "")
    {
        _assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
        string assemblyName = _assembly.GetName().Name ?? string.Empty;
        string root = Normalize(rootPath);
        _prefix = string.IsNullOrEmpty(root) ? assemblyName + "." : assemblyName + "." + root.Replace('/', '.') + ".";
    }

    /// <inheritdoc />
    public IFileInfo GetFileInfo(string? subpath)
    {
        string relative = Normalize(subpath);
        if (relative.Length == 0)
        {
            return new NotFoundFileInfo(subpath ?? string.Empty);
        }

        string resourceName = _prefix + relative.Replace('/', '.');
        return _assembly.GetManifestResourceInfo(resourceName) is null
            ? new NotFoundFileInfo(relative)
            : new SnowEmbeddedFileInfo(_assembly, resourceName, Path.GetFileName(relative));
    }

    /// <inheritdoc />
    public IDirectoryContents GetDirectoryContents(string? subpath)
    {
        string relative = Normalize(subpath);
        string prefix = relative.Length == 0 ? _prefix : _prefix + relative.Replace('/', '.') + ".";

        List<string> children = _assembly
            .GetManifestResourceNames()
            .Where(name => name.StartsWith(prefix, StringComparison.Ordinal))
            .Select(name => name[prefix.Length..])
            .ToList();

        if (children.Count == 0)
        {
            return NotFoundDirectoryContents.Singleton;
        }

        HashSet<string> fileNames = new HashSet<string>(children, StringComparer.Ordinal);
        SortedSet<string> firstSegments = new SortedSet<string>(StringComparer.Ordinal);
        foreach (string child in children)
        {
            int dot = child.IndexOf('.');
            firstSegments.Add(dot < 0 ? child : child[..dot]);
        }

        List<IFileInfo> entries = new List<IFileInfo>();
        foreach (string segment in firstSegments)
        {
            entries.Add(fileNames.Contains(segment)
                ? new SnowEmbeddedFileInfo(_assembly, prefix + segment, segment)
                : new SnowEmbeddedDirectoryInfo(segment));
        }

        return new SnowDirectoryContents(entries);
    }

    /// <inheritdoc />
    public IChangeToken Watch(string filter) => NullChangeToken.Singleton;

    private static string Normalize(string? path) => (path ?? string.Empty).Replace('\\', '/').Trim('/');
}

internal sealed class SnowEmbeddedFileInfo : IFileInfo
{
    private readonly Assembly _assembly;
    private readonly string _resourceName;

    public SnowEmbeddedFileInfo(Assembly assembly, string resourceName, string name)
    {
        _assembly = assembly;
        _resourceName = resourceName;
        Name = name;
        using Stream? stream = _assembly.GetManifestResourceStream(_resourceName);
        Length = stream?.Length ?? 0;
    }

    public bool Exists => true;

    public long Length { get; }

    public string? PhysicalPath => null;

    public string Name { get; }

    public DateTimeOffset LastModified => DateTimeOffset.MinValue;

    public bool IsDirectory => false;

    public Stream CreateReadStream() => _assembly.GetManifestResourceStream(_resourceName)
        ?? throw new FileNotFoundException("内嵌资源不存在: " + _resourceName);
}

internal sealed class SnowEmbeddedDirectoryInfo : IFileInfo
{
    public SnowEmbeddedDirectoryInfo(string name) => Name = name;

    public bool Exists => true;

    public long Length => 0;

    public string? PhysicalPath => null;

    public string Name { get; }

    public DateTimeOffset LastModified => DateTimeOffset.MinValue;

    public bool IsDirectory => true;

    public Stream CreateReadStream() => throw new InvalidOperationException("目录不能按文件读取。");
}

internal sealed class SnowDirectoryContents : IDirectoryContents
{
    private readonly IReadOnlyList<IFileInfo> _entries;

    public SnowDirectoryContents(IReadOnlyList<IFileInfo> entries) => _entries = entries;

    public bool Exists => true;

    public IEnumerator<IFileInfo> GetEnumerator() => _entries.GetEnumerator();

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => _entries.GetEnumerator();
}

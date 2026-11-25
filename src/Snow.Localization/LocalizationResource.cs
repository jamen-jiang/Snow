using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Snow.Localization.VirtualFiles.Json;
using Snow.VirtualFileSystem;

namespace Snow.Localization;

/// <summary>
/// 一个本地化资源(对应 ABP 的 <c>LocalizationResource</c>): 资源名、父资源名, 以及若干 json 虚拟路径。
/// <para>只做 json: <c>AddVirtualJson</c> 先记下目录, 首次取值时一次加载并缓存。</para>
/// </summary>
public class LocalizationResource
{
    private static readonly StringComparer CultureComparer = StringComparer.OrdinalIgnoreCase;

    private readonly object _syncObj = new();
    private readonly List<string> _virtualJsonPaths = [];

    private Dictionary<string, Dictionary<string, LocalizedString>>? _texts;
    private bool _loaded;

    /// <summary>构造器</summary>
    public LocalizationResource()
    {
        ResourceName = string.Empty;
    }

    /// <summary>构造器</summary>
    public LocalizationResource(string resourceName, string? defaultCultureName = null)
    {
        ResourceName = resourceName;
        DefaultCultureName = defaultCultureName;
    }

    /// <summary>构造器: 资源名取自 <see cref="LocalizationResourceNameAttribute"/></summary>
    public LocalizationResource(Type resourceType, string? defaultCultureName = null)
    {
        ArgumentNullException.ThrowIfNull(resourceType);

        ResourceName = LocalizationResourceNameAttribute.GetName(resourceType);
        DefaultCultureName = defaultCultureName;
        ResourceType = resourceType;
    }

    /// <summary>资源名</summary>
    public string ResourceName { get; }

    /// <summary>继承的父资源名</summary>
    public List<string> BaseResourceNames { get; } = [];

    /// <summary>默认文化</summary>
    public string? DefaultCultureName { get; set; }

    /// <summary>资源类型</summary>
    public Type? ResourceType { get; }

    /// <summary>追加一个 json 虚拟路径(由 <c>AddVirtualJson</c> 调用)</summary>
    internal void AddVirtualJson(string virtualPath)
    {
        _virtualJsonPaths.Add(virtualPath);
    }

    /// <summary>加载 json 文件(每个资源只加载一次)</summary>
    internal void EnsureLoaded(IServiceProvider serviceProvider)
    {
        if (_loaded)
        {
            return;
        }

        lock (_syncObj)
        {
            if (_loaded)
            {
                return;
            }

            var texts = new Dictionary<string, Dictionary<string, LocalizedString>>(CultureComparer);

            if (_virtualJsonPaths.Count > 0)
            {
                IVirtualFileProvider virtualFileProvider = serviceProvider.GetRequiredService<IVirtualFileProvider>();

                foreach (string virtualPath in _virtualJsonPaths)
                {
                    JsonVirtualFileLocalizationLoader.Load(virtualPath, virtualFileProvider, texts);
                }
            }

            _texts = texts;
            _loaded = true;
        }
    }

    /// <summary>取指定文化下的一条文本</summary>
    internal LocalizedString? GetOrNull(string cultureName, string name)
    {
        if (_texts is null || !_texts.TryGetValue(cultureName, out Dictionary<string, LocalizedString>? texts))
        {
            return null;
        }

        return texts.GetValueOrDefault(name);
    }

    /// <summary>把指定文化的全部文本填进字典</summary>
    internal void Fill(string cultureName, Dictionary<string, LocalizedString> dictionary)
    {
        if (_texts is null || !_texts.TryGetValue(cultureName, out Dictionary<string, LocalizedString>? texts))
        {
            return;
        }

        foreach (KeyValuePair<string, LocalizedString> pair in texts)
        {
            dictionary[pair.Key] = pair.Value;
        }
    }
}

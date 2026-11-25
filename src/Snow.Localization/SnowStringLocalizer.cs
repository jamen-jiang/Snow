using System.Globalization;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace Snow.Localization;

/// <summary>
/// 基于选项资源的字符串本地化器: 按资源及其父资源依次查 json 文本
/// </summary>
public sealed class SnowStringLocalizer : IStringLocalizer
{
    private readonly string _resourceName;
    private readonly IOptions<SnowLocalizationOptions> _options;
    private readonly IServiceProvider _serviceProvider;

    /// <summary>构造器</summary>
    public SnowStringLocalizer(string resourceName, IOptions<SnowLocalizationOptions> options, IServiceProvider serviceProvider)
    {
        _resourceName = resourceName;
        _options = options;
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public LocalizedString this[string name]
    {
        get
        {
            LocalizedString? value = Find(name);
            return value ?? new LocalizedString(name, name, resourceNotFound: true);
        }
    }

    /// <inheritdoc />
    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            LocalizedString? format = Find(name);
            if (format is null)
            {
                return new LocalizedString(name, name, resourceNotFound: true);
            }

            return new LocalizedString(name, string.Format(CultureInfo.CurrentCulture, format.Value, arguments), resourceNotFound: false);
        }
    }

    /// <inheritdoc />
    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
    {
        foreach (LocalizationResource resource in ResolveResources())
        {
            var texts = new Dictionary<string, LocalizedString>(StringComparer.Ordinal);
            foreach (string culture in ResolveCultureCandidates(resource, includeParentCultures))
            {
                resource.Fill(culture, texts);
            }

            foreach (LocalizedString value in texts.Values)
            {
                yield return value;
            }
        }
    }

    private LocalizedString? Find(string name)
    {
        foreach (LocalizationResource resource in ResolveResources())
        {
            foreach (string culture in ResolveCultureCandidates(resource, includeParentCultures: true))
            {
                LocalizedString? value = resource.GetOrNull(culture, name);
                if (value is not null)
                {
                    return value;
                }
            }
        }

        return null;
    }

    private IEnumerable<LocalizationResource> ResolveResources()
    {
        SnowLocalizationOptions options = _options.Value;
        var queue = new Queue<LocalizationResource>();

        if (options.Resources.TryGetValue(_resourceName, out LocalizationResource? resource))
        {
            queue.Enqueue(resource);
        }
        else if (options.DefaultResourceType is not null)
        {
            string defaultResourceName = LocalizationResourceNameAttribute.GetName(options.DefaultResourceType);
            if (options.Resources.TryGetValue(defaultResourceName, out LocalizationResource? fallback))
            {
                queue.Enqueue(fallback);
            }
        }

        var visited = new HashSet<LocalizationResource>();
        while (queue.Count > 0)
        {
            LocalizationResource current = queue.Dequeue();
            if (!visited.Add(current))
            {
                continue;
            }

            current.EnsureLoaded(_serviceProvider);
            yield return current;

            foreach (string baseResourceName in current.BaseResourceNames)
            {
                if (options.Resources.TryGetValue(baseResourceName, out LocalizationResource? baseResource))
                {
                    queue.Enqueue(baseResource);
                }
            }
        }
    }

    private IEnumerable<string> ResolveCultureCandidates(LocalizationResource resource, bool includeParentCultures)
    {
        SnowLocalizationOptions options = _options.Value;
        string currentCultureName = CultureInfo.CurrentUICulture.Name;

        yield return currentCultureName;

        if (includeParentCultures && options.TryToGetFromBaseCulture && currentCultureName.Contains('-'))
        {
            int separatorIndex = currentCultureName.IndexOf('-');
            if (separatorIndex > 0)
            {
                yield return currentCultureName[..separatorIndex];
            }
        }

        if (includeParentCultures && options.TryToGetFromDefaultCulture && !resource.DefaultCultureName.IsNullOrEmpty())
        {
            yield return resource.DefaultCultureName;
        }
    }
}
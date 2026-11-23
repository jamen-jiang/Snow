using System.Globalization;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace Snow.Localization;

/// <summary>
/// 基于选项资源的字符串本地化器
/// </summary>
public sealed class SnowStringLocalizer : IStringLocalizer
{
    private readonly string _resourceName;
    private readonly IOptions<SnowLocalizationOptions> _options;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="resourceName">资源名</param>
    /// <param name="options">本地化选项</param>
    public SnowStringLocalizer(string resourceName, IOptions<SnowLocalizationOptions> options)
    {
        _resourceName = resourceName;
        _options = options;
    }

    /// <inheritdoc />
    public LocalizedString this[string name]
    {
        get
        {
            string? value = Find(name);
            return new LocalizedString(name, value ?? name, resourceNotFound: value is null);
        }
    }

    /// <inheritdoc />
    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            string? format = Find(name);
            string value = format is null
                ? name
                : string.Format(CultureInfo.CurrentCulture, format, arguments);
            return new LocalizedString(name, value, resourceNotFound: format is null);
        }
    }

    /// <inheritdoc />
    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
    {
        LocalizationResource? resource = ResolveResource();
        if (resource is null)
        {
            yield break;
        }

        string culture = ResolveCulture(resource);
        if (!resource.Texts.TryGetValue(culture, out Dictionary<string, string>? texts))
        {
            yield break;
        }

        foreach (KeyValuePair<string, string> pair in texts)
        {
            yield return new LocalizedString(pair.Key, pair.Value, resourceNotFound: false);
        }
    }

    private string? Find(string name)
    {
        LocalizationResource? resource = ResolveResource();
        if (resource is null)
        {
            return null;
        }

        foreach (string culture in ResolveCultureCandidates(resource))
        {
            if (resource.Texts.TryGetValue(culture, out Dictionary<string, string>? texts)
                && texts.TryGetValue(name, out string? value))
            {
                return value;
            }
        }

        return null;
    }

    private LocalizationResource? ResolveResource()
    {
        SnowLocalizationOptions options = _options.Value;
        if (options.Resources.TryGetValue(_resourceName, out LocalizationResource? resource))
        {
            return resource;
        }

        return options.Resources.TryGetValue(options.DefaultResourceName, out LocalizationResource? fallback) ? fallback : null;
    }

    private string ResolveCulture(LocalizationResource resource)
        => ResolveCultureCandidates(resource).First();

    private IEnumerable<string> ResolveCultureCandidates(LocalizationResource resource)
    {
        CultureInfo current = CultureInfo.CurrentUICulture;

        yield return current.Name;

        if (current.Parent is not null && !string.IsNullOrEmpty(current.Parent.Name) && current.Parent.Name != current.Name)
        {
            yield return current.Parent.Name;
        }

        if (!string.IsNullOrWhiteSpace(resource.DefaultCulture))
        {
            yield return resource.DefaultCulture;
        }

        if (!string.IsNullOrWhiteSpace(_options.Value.DefaultCulture))
        {
            yield return _options.Value.DefaultCulture;
        }
    }
}

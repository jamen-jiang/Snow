using Snow.Exceptions;

namespace Snow.Localization;

/// <summary>
/// 本地化资源字典(对应 ABP 的 <c>LocalizationResourceDictionary</c>)
/// </summary>
public class LocalizationResourceDictionary : Dictionary<string, LocalizationResource>
{
    /// <summary>构造器: 资源名大小写不敏感</summary>
    public LocalizationResourceDictionary()
        : base(StringComparer.OrdinalIgnoreCase)
    {
    }

    /// <summary>按资源类型追加资源并指定默认文化</summary>
    public LocalizationResource Add<TResource>(string? defaultCultureName = null)
    {
        return Add(typeof(TResource), defaultCultureName);
    }

    /// <summary>按资源类型追加资源并指定默认文化</summary>
    public LocalizationResource Add(Type resourceType, string? defaultCultureName = null)
    {
        ArgumentNullException.ThrowIfNull(resourceType);

        string name = LocalizationResourceNameAttribute.GetName(resourceType);
        if (ContainsKey(name))
        {
            throw new SnowException("该资源已被添加过: " + resourceType.AssemblyQualifiedName);
        }

        var resource = new LocalizationResource(resourceType, defaultCultureName);
        this[name] = resource;

        return resource;
    }
}
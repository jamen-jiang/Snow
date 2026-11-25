namespace Snow.Localization;

/// <summary>
/// 本地化资源扩展(对应 ABP 的 <c>LocalizationResourceExtensions</c>)
/// </summary>
public static class LocalizationResourceExtensions
{
    /// <summary>
    /// 追加 json 虚拟文件目录(对应 ABP 的 <c>AddVirtualJson</c>)
    /// <para>只认目录里的 *.json, 文件名不限, 文化名取自文件里的 <c>Culture</c> 字段。</para>
    /// </summary>
    public static TLocalizationResource AddVirtualJson<TLocalizationResource>(
        this TLocalizationResource localizationResource,
        string virtualPath)
        where TLocalizationResource : LocalizationResource
    {
        ArgumentNullException.ThrowIfNull(localizationResource);
        ArgumentException.ThrowIfNullOrWhiteSpace(virtualPath);

        localizationResource.AddVirtualJson(virtualPath.EnsureStartsWith('/'));

        return localizationResource;
    }

    /// <summary>追加父资源类型(对应 ABP 的 <c>AddBaseTypes</c>)</summary>
    public static TLocalizationResource AddBaseTypes<TLocalizationResource>(
        this TLocalizationResource localizationResource,
        params Type[] types)
        where TLocalizationResource : LocalizationResource
    {
        ArgumentNullException.ThrowIfNull(localizationResource);
        ArgumentNullException.ThrowIfNull(types);

        foreach (Type type in types)
        {
            localizationResource.BaseResourceNames.AddIfNotContains(LocalizationResourceNameAttribute.GetName(type));
        }

        return localizationResource;
    }

    /// <summary>追加父资源名(对应 ABP 的 <c>AddBaseResources</c>)</summary>
    public static TLocalizationResource AddBaseResources<TLocalizationResource>(
        this TLocalizationResource localizationResource,
        params string[] baseResourceNames)
        where TLocalizationResource : LocalizationResource
    {
        ArgumentNullException.ThrowIfNull(localizationResource);
        ArgumentNullException.ThrowIfNull(baseResourceNames);

        foreach (string name in baseResourceNames)
        {
            localizationResource.BaseResourceNames.AddIfNotContains(name);
        }

        return localizationResource;
    }
}

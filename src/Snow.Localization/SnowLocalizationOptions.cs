namespace Snow.Localization;

/// <summary>
/// 本地化选项(对应 ABP 的 <c>AbpLocalizationOptions</c>)
/// </summary>
public class SnowLocalizationOptions
{
    /// <summary>
    /// 资源名 → 资源; 资源名大小写不敏感
    /// </summary>
    public LocalizationResourceDictionary Resources { get; }

    /// <summary>
    /// 默认资源类型; 为空表示没有默认资源
    /// </summary>
    public Type? DefaultResourceType { get; set; }

    /// <summary>
    /// 找不到当前文化时是否回退到其父(基础)文化
    /// </summary>
    public bool TryToGetFromBaseCulture { get; set; }

    /// <summary>
    /// 找不到当前文化时是否回退到资源的默认文化
    /// </summary>
    public bool TryToGetFromDefaultCulture { get; set; }

    /// <summary>
    /// 构造器
    /// </summary>
    public SnowLocalizationOptions()
    {
        Resources = new LocalizationResourceDictionary();
        TryToGetFromBaseCulture = true;
        TryToGetFromDefaultCulture = true;
    }
}

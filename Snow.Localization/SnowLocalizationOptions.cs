namespace Snow.Localization;

/// <summary>
/// 本地化选项: 资源表与默认文化
/// </summary>
public sealed class SnowLocalizationOptions
{
    /// <summary>
    /// 找不到对应资源时回退到的资源名
    /// </summary>
    public string DefaultResourceName { get; set; } = "Default";

    /// <summary>
    /// 全局默认文化; 为空时用进程当前 UI 文化
    /// </summary>
    public string? DefaultCulture { get; set; }

    /// <summary>
    /// 声明的支持文化(仅作文档用途, 请求文化解析交给宿主)
    /// </summary>
    public List<string> SupportedCultures { get; } = new();

    /// <summary>
    /// 资源名 → 资源; 资源名大小写不敏感
    /// </summary>
    public Dictionary<string, LocalizationResource> Resources { get; } = new(StringComparer.OrdinalIgnoreCase);
}

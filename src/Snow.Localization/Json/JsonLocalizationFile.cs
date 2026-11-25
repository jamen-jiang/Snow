namespace Snow.Localization.Json;

/// <summary>
/// 本地化 json 文件模型(对应 ABP 的 <c>JsonLocalizationFile</c>)
/// </summary>
public class JsonLocalizationFile
{
    /// <summary>文化名, 如 en / zh-Hans</summary>
    public string Culture { get; set; } = string.Empty;

    /// <summary>文本</summary>
    public Dictionary<string, object?> Texts { get; set; } = new();
}

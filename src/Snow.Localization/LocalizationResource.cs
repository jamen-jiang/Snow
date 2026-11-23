namespace Snow.Localization;

/// <summary>
/// 一个本地化资源: 按文化存放「键 → 文本」
/// </summary>
public sealed class LocalizationResource
{
    /// <summary>
    /// 该资源的默认文化(不填则用全局默认)
    /// </summary>
    public string? DefaultCulture { get; set; }

    /// <summary>
    /// 文化名 → (键 → 文本); 文化名大小写不敏感
    /// </summary>
    public Dictionary<string, Dictionary<string, string>> Texts { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 追加一条文本
    /// </summary>
    /// <param name="culture">文化名, 如 "zh-CN"</param>
    /// <param name="key">键</param>
    /// <param name="value">文本</param>
    /// <returns>资源本身, 便于链式调用</returns>
    public LocalizationResource Add(string culture, string key, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(culture);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (!Texts.TryGetValue(culture, out Dictionary<string, string>? texts))
        {
            texts = new Dictionary<string, string>(StringComparer.Ordinal);
            Texts[culture] = texts;
        }

        texts[key] = value;
        return this;
    }
}

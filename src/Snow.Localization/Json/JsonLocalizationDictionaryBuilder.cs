using System.Text.Json;
using Microsoft.Extensions.Localization;
using Snow.Exceptions;

namespace Snow.Localization.Json;

/// <summary>
/// 从 json 构建本地化字典(对应 ABP 的 <c>JsonLocalizationDictionaryBuilder</c>)
/// </summary>
public static class JsonLocalizationDictionaryBuilder
{
    private static readonly JsonSerializerOptions DeserializeOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    /// <summary>从 json 字符串构建; 没有 culture 时返回 null</summary>
    /// <param name="jsonString">Json 字符串</param>
    public static (string CultureName, Dictionary<string, LocalizedString> Texts)? BuildFromJsonString(string jsonString)
    {
        JsonLocalizationFile? file;
        try
        {
            file = JsonSerializer.Deserialize<JsonLocalizationFile>(jsonString, DeserializeOptions);
        }
        catch (JsonException ex)
        {
            throw new SnowException("无法解析 json 字符串。 " + ex.Message);
        }

        if (file == null || string.IsNullOrEmpty(file.Culture))
        {
            return null;
        }

        var texts = new Dictionary<string, LocalizedString>(StringComparer.Ordinal);
        foreach (KeyValuePair<string, string> item in FlattenTexts(file.Texts))
        {
            if (string.IsNullOrEmpty(item.Key))
            {
                throw new SnowException("给定 json 字符串中的键为空。");
            }

            texts[item.Key] = new LocalizedString(item.Key, item.Value.NormalizeLineEndings());
        }

        return (file.Culture, texts);
    }

    private static Dictionary<string, string> FlattenTexts(Dictionary<string, object?> texts, string prefix = "")
    {
        var dictionary = new Dictionary<string, string>();
        foreach (KeyValuePair<string, object?> pair in texts)
        {
            string key = string.IsNullOrEmpty(prefix) ? pair.Key : prefix + "__" + pair.Key;
            object? value = pair.Value;

            if (value is not JsonElement element)
            {
                dictionary[key] = value switch
                {
                    null => string.Empty,
                    string text => text,
                    _ => value.ToString() ?? string.Empty,
                };
                continue;
            }

            foreach (KeyValuePair<string, string> item in FlattenJsonElement(element, key))
            {
                dictionary[item.Key] = item.Value;
            }
        }

        return dictionary;
    }

    private static IEnumerable<KeyValuePair<string, string>> FlattenJsonElement(JsonElement element, string prefix)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                yield return new KeyValuePair<string, string>(prefix, element.GetString() ?? string.Empty);
                break;
            case JsonValueKind.Object:
                foreach (JsonProperty property in element.EnumerateObject())
                {
                    string childPrefix = prefix + "__" + property.Name;
                    foreach (KeyValuePair<string, string> item in FlattenJsonElement(property.Value, childPrefix))
                    {
                        yield return item;
                    }
                }

                break;
            case JsonValueKind.Array:
                int index = 0;
                foreach (JsonElement item in element.EnumerateArray())
                {
                    string childPrefix = $"{prefix}__{index}";
                    foreach (KeyValuePair<string, string> child in FlattenJsonElement(item, childPrefix))
                    {
                        yield return child;
                    }

                    index++;
                }

                break;
            default:
                yield return new KeyValuePair<string, string>(prefix, element.ToString());
                break;
        }
    }
}

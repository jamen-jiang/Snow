using System.Globalization;

namespace Rongia.Expressions;

/// <summary>
/// 表达式结果的类型转换辅助
/// </summary>
internal static class SpelValue
{
    /// <summary>
    /// 转布尔: null/false 为假, 字符串按 bool.TryParse
    /// </summary>
    /// <param name="value">待转换值</param>
    /// <returns>布尔结果</returns>
    public static bool ToBoolean(object? value) => value switch
    {
        null => false,
        bool boolean => boolean,
        string text => bool.TryParse(text, out bool parsed) && parsed,
        IConvertible convertible => Convert.ToBoolean(convertible, CultureInfo.InvariantCulture),
        _ => true
    };

    /// <summary>
    /// 转不变文化字符串: null 为空串
    /// </summary>
    /// <param name="value">待转换值</param>
    /// <returns>字符串结果</returns>
    public static string ToInvariantString(object? value) => value switch
    {
        null => string.Empty,
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? string.Empty
    };
}

using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Snow;

namespace System;

/// <summary>
/// 针对字符串的扩展方法。
/// </summary>
public static class SnowStringExtensions
{
    /// <summary>
    /// 如果字符串结尾不是这个字符, 就把它补到末尾。
    /// </summary>
    /// <param name="str">目标字符串</param>
    /// <param name="c">要保证出现在末尾的字符</param>
    /// <param name="comparisonType">比较方式, 默认 Ordinal</param>
    /// <returns>补好后的字符串; 本来就以该字符结尾时原样返回。</returns>
    public static string EnsureEndsWith(this string str, char c, StringComparison comparisonType = StringComparison.Ordinal)
    {
        Check.NotNull(str, nameof(str));

        if (str.EndsWith(c.ToString(), comparisonType))
        {
            return str;
        }

        return str + c;
    }

    /// <summary>
    /// 如果字符串开头不是这个字符, 就把它补到开头。
    /// </summary>
    /// <param name="str">目标字符串</param>
    /// <param name="c">要保证出现在开头的字符</param>
    /// <param name="comparisonType">比较方式, 默认 Ordinal</param>
    /// <returns>补好后的字符串; 本来就以该字符开头时原样返回。</returns>
    public static string EnsureStartsWith(this string str, char c, StringComparison comparisonType = StringComparison.Ordinal)
    {
        Check.NotNull(str, nameof(str));

        if (str.StartsWith(c.ToString(), comparisonType))
        {
            return str;
        }

        return c + str;
    }

    /// <summary>
    /// 判断字符串是 null 还是空串。
    /// </summary>
    /// <param name="str">要判断的字符串</param>
    public static bool IsNullOrEmpty([System.Diagnostics.CodeAnalysis.NotNullWhen(false)]this string? str)
    {
        return string.IsNullOrEmpty(str);
    }

    /// <summary>
    /// 判断字符串是 null、空串, 还是只由空白字符组成。
    /// </summary>
    /// <param name="str">要判断的字符串</param>
    public static bool IsNullOrWhiteSpace([System.Diagnostics.CodeAnalysis.NotNullWhen(false)]this string? str)
    {
        return string.IsNullOrWhiteSpace(str);
    }

    /// <summary>
    /// 从字符串开头截取指定长度的子串。
    /// </summary>
    /// <param name="str">目标字符串</param>
    /// <param name="len">要截取的长度</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="str"/> 为 null 时抛出</exception>
    /// <exception cref="ArgumentException">当 <paramref name="len"/> 大于字符串长度时抛出</exception>
    public static string Left(this string str, int len)
    {
        Check.NotNull(str, nameof(str));

        if (str.Length < len)
        {
            throw new ArgumentException("len argument can not be bigger than given string's length!");
        }

        return str.Substring(0, len);
    }

    /// <summary>
    /// 把字符串里的换行符统一成 <see cref="Environment.NewLine"/>。
    /// </summary>
    /// <param name="str">目标字符串</param>
    public static string NormalizeLineEndings(this string str)
    {
        return str.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", Environment.NewLine);
    }

    /// <summary>
    /// 取某个字符在字符串里第 n 次出现的位置。
    /// </summary>
    /// <param name="str">要搜索的源字符串</param>
    /// <param name="c">要查找的字符</param>
    /// <param name="n">第几次出现, 从 1 开始</param>
    /// <returns>字符下标; 出现次数不足 n 次时返回 -1。</returns>
    public static int NthIndexOf(this string str, char c, int n)
    {
        Check.NotNull(str, nameof(str));

        var count = 0;
        for (var i = 0; i < str.Length; i++)
        {
            if (str[i] != c)
            {
                continue;
            }

            if ((++count) == n)
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// 从字符串末尾去掉最先匹配到的那个后缀。
    /// </summary>
    /// <param name="str">目标字符串</param>
    /// <param name="postFixes">一个或多个候选后缀</param>
    /// <returns>去掉后缀后的字符串; 一个后缀都没命中时原样返回。</returns>
    public static string RemovePostFix(this string str, params string[] postFixes)
    {
        return str.RemovePostFix(StringComparison.Ordinal, postFixes);
    }

    /// <summary>
    /// 从字符串末尾去掉最先匹配到的那个后缀。
    /// </summary>
    /// <param name="str">目标字符串</param>
    /// <param name="comparisonType">比较方式</param>
    /// <param name="postFixes">一个或多个候选后缀</param>
    /// <returns>去掉后缀后的字符串; 一个后缀都没命中时原样返回。</returns>
    public static string RemovePostFix(this string str, StringComparison comparisonType, params string[] postFixes)
    {
        if (str.IsNullOrEmpty())
        {
            return str;
        }

        if (postFixes.IsNullOrEmpty())
        {
            return str;
        }

        foreach (var postFix in postFixes)
        {
            if (str.EndsWith(postFix, comparisonType))
            {
                return str.Left(str.Length - postFix.Length);
            }
        }

        return str;
    }

    /// <summary>
    /// 从字符串开头去掉最先匹配到的那个前缀。
    /// </summary>
    /// <param name="str">目标字符串</param>
    /// <param name="preFixes">一个或多个候选前缀</param>
    /// <returns>去掉前缀后的字符串; 一个前缀都没命中时原样返回。</returns>
    public static string RemovePreFix(this string str, params string[] preFixes)
    {
        return str.RemovePreFix(StringComparison.Ordinal, preFixes);
    }

    /// <summary>
    /// 从字符串开头去掉最先匹配到的那个前缀。
    /// </summary>
    /// <param name="str">目标字符串</param>
    /// <param name="comparisonType">比较方式</param>
    /// <param name="preFixes">一个或多个候选前缀</param>
    /// <returns>去掉前缀后的字符串; 一个前缀都没命中时原样返回。</returns>
    public static string RemovePreFix(this string str, StringComparison comparisonType, params string[] preFixes)
    {
        if (str.IsNullOrEmpty())
        {
            return str;
        }

        if (preFixes.IsNullOrEmpty())
        {
            return str;
        }

        foreach (var preFix in preFixes)
        {
            if (str.StartsWith(preFix, comparisonType))
            {
                return str.Right(str.Length - preFix.Length);
            }
        }

        return str;
    }

    /// <summary>
    /// 只替换第一次出现的匹配内容, 一次扫描完成拼接(短串走栈上缓冲)。
    /// </summary>
    /// <param name="str">目标字符串</param>
    /// <param name="search">要查找的内容</param>
    /// <param name="replace">替换成的内容</param>
    /// <param name="comparisonType">比较方式, 默认 Ordinal</param>
    /// <returns>替换后的字符串; 找不到该内容时原样返回。</returns>
    public static string ReplaceFirst(this string str, string search, string replace, StringComparison comparisonType = StringComparison.Ordinal)
    {
        Check.NotNull(str, nameof(str));

        var pos = str.IndexOf(search, comparisonType);
        if (pos < 0)
        {
            return str;
        }

        var searchLength = search.Length;
        var replaceLength = replace.Length;
        var newLength = str.Length - searchLength + replaceLength;

        Span<char> buffer = newLength <= 1024 ? stackalloc char[newLength] : new char[newLength];

        // 先复制匹配位置之前的原文
        str.AsSpan(0, pos).CopyTo(buffer);

        // 再复制替换内容
        replace.AsSpan().CopyTo(buffer.Slice(pos));

        // 最后复制原文剩下的部分
        str.AsSpan(pos + searchLength).CopyTo(buffer.Slice(pos + replaceLength));

        return buffer.ToString();
    }

    /// <summary>
    /// 从字符串末尾截取指定长度的子串。
    /// </summary>
    /// <param name="str">目标字符串</param>
    /// <param name="len">要截取的长度</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="str"/> 为 null 时抛出</exception>
    /// <exception cref="ArgumentException">当 <paramref name="len"/> 大于字符串长度时抛出</exception>
    public static string Right(this string str, int len)
    {
        Check.NotNull(str, nameof(str));

        if (str.Length < len)
        {
            throw new ArgumentException("len argument can not be bigger than given string's length!");
        }

        return str.Substring(str.Length - len, len);
    }

    /// <summary>
    /// 用指定的分隔字符串调用 string.Split。
    /// </summary>
    /// <param name="str">目标字符串</param>
    /// <param name="separator">分隔字符串</param>
    public static string[] Split(this string str, string separator)
    {
        return str.Split(new[] { separator }, StringSplitOptions.None);
    }

    /// <summary>
    /// 用指定的分隔字符串调用 string.Split, 并指定拆分选项。
    /// </summary>
    /// <param name="str">目标字符串</param>
    /// <param name="separator">分隔字符串</param>
    /// <param name="options">拆分选项</param>
    public static string[] Split(this string str, string separator, StringSplitOptions options)
    {
        return str.Split(new[] { separator }, options);
    }

    /// <summary>
    /// 按 <see cref="Environment.NewLine"/> 拆成多行。
    /// </summary>
    /// <param name="str">目标字符串</param>
    public static string[] SplitToLines(this string str)
    {
        return str.Split(Environment.NewLine);
    }

    /// <summary>
    /// 按 <see cref="Environment.NewLine"/> 拆成多行, 并指定拆分选项。
    /// </summary>
    /// <param name="str">目标字符串</param>
    /// <param name="options">拆分选项</param>
    public static string[] SplitToLines(this string str, StringSplitOptions options)
    {
        return str.Split(Environment.NewLine, options);
    }

    /// <summary>
    /// 把 PascalCase 转成 camelCase(首字母小写)。
    /// </summary>
    /// <param name="str">要转换的字符串</param>
    /// <param name="useCurrentCulture">true 用当前区域文化, 否则用不变文化</param>
    /// <param name="handleAbbreviations">true 时把全大写的缩写(如 XYZ)整体转成小写</param>
    /// <returns>camelCase 形式的字符串</returns>
    public static string ToCamelCase(this string str, bool useCurrentCulture = false, bool handleAbbreviations = false)
    {
        if (string.IsNullOrWhiteSpace(str))
        {
            return str;
        }

        if (str.Length == 1)
        {
            return useCurrentCulture ? str.ToLower() : str.ToLowerInvariant();
        }

        if (handleAbbreviations && IsAllUpperCase(str))
        {
            return useCurrentCulture ? str.ToLower() : str.ToLowerInvariant();
        }

        return (useCurrentCulture ? char.ToLower(str[0]) : char.ToLowerInvariant(str[0])) + str.Substring(1);
    }

    /// <summary>
    /// 把 PascalCase/camelCase 转成以空格分词的句子。
    /// 例: "ThisIsSampleSentence" 转成 "This is a sample sentence"。
    /// </summary>
    /// <param name="str">要转换的字符串</param>
    /// <param name="useCurrentCulture">true 用当前区域文化, 否则用不变文化</param>
    public static string ToSentenceCase(this string str, bool useCurrentCulture = false)
    {
        if (string.IsNullOrWhiteSpace(str))
        {
            return str;
        }

        return useCurrentCulture
            ? Regex.Replace(str, "[a-z][A-Z]", m => m.Value[0] + " " + char.ToLower(m.Value[1]))
            : Regex.Replace(str, "[a-z][A-Z]", m => m.Value[0] + " " + char.ToLowerInvariant(m.Value[1]));
    }

    /// <summary>
    /// 把 PascalCase/camelCase 转成 kebab-case(短横线分隔)。
    /// </summary>
    /// <param name="str">要转换的字符串</param>
    /// <param name="useCurrentCulture">true 用当前区域文化, 否则用不变文化</param>
    public static string ToKebabCase(this string str, bool useCurrentCulture = false)
    {
        if (string.IsNullOrWhiteSpace(str))
        {
            return str;
        }

        str = str.ToCamelCase();

        return useCurrentCulture
            ? Regex.Replace(str, "[a-z][A-Z]", m => m.Value[0] + "-" + char.ToLower(m.Value[1]))
            : Regex.Replace(str, "[a-z][A-Z]", m => m.Value[0] + "-" + char.ToLowerInvariant(m.Value[1]));
    }

    /// <summary>
    /// 把 PascalCase/camelCase 转成 snake_case(下划线分隔)。
    /// 例: "ThisIsSampleSentence" 转成 "this_is_a_sample_sentence"。
    /// 实现参考 https://github.com/npgsql/npgsql/blob/dev/src/Npgsql/NameTranslation/NpgsqlSnakeCaseNameTranslator.cs#L51
    /// </summary>
    /// <param name="str">要转换的字符串</param>
    /// <returns>snake_case 形式的字符串</returns>
    public static string ToSnakeCase(this string str)
    {
        return str.IsNullOrWhiteSpace() ? str : JsonNamingPolicy.SnakeCaseLower.ConvertName(str);
    }

    /// <summary>
    /// 把字符串解析成枚举值, 区分大小写。
    /// </summary>
    /// <typeparam name="T">枚举类型</typeparam>
    /// <param name="value">要转换的字符串</param>
    /// <returns>解析出的枚举值</returns>
    public static T ToEnum<T>(this string value)
        where T : struct
    {
        Check.NotNull(value, nameof(value));
        return (T)Enum.Parse(typeof(T), value);
    }

    /// <summary>
    /// 把字符串解析成枚举值。
    /// </summary>
    /// <typeparam name="T">枚举类型</typeparam>
    /// <param name="value">要转换的字符串</param>
    /// <param name="ignoreCase">是否忽略大小写</param>
    /// <returns>解析出的枚举值</returns>
    public static T ToEnum<T>(this string value, bool ignoreCase)
        where T : struct
    {
        Check.NotNull(value, nameof(value));
        return (T)Enum.Parse(typeof(T), value, ignoreCase);
    }

    /// <summary>
    /// 计算字符串 UTF-8 字节的 MD5, 返回大写十六进制串。
    /// </summary>
    /// <param name="str">目标字符串</param>
    public static string ToMd5(this string str)
    {
#if NETSTANDARD2_0 || NETSTANDARD2_1
        using (var md5 = MD5.Create())
        {
            var inputBytes = Encoding.UTF8.GetBytes(str);
            var hashBytes = md5.ComputeHash(inputBytes);

            var sb = new StringBuilder();
            foreach (var hashByte in hashBytes)
            {
                sb.Append(hashByte.ToString("X2"));
            }

            return sb.ToString();
        }
#else
        return Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(str)));
#endif
    }

    /// <summary>
    /// 计算字符串 UTF-8 字节的 SHA256, 返回小写十六进制串。
    /// </summary>
    /// <param name="str">目标字符串</param>
    public static string ToSha256(this string str)
    {
#if NETSTANDARD2_0 || NETSTANDARD2_1
        using (var sha = SHA256.Create())
        {
            var data = sha.ComputeHash(Encoding.UTF8.GetBytes(str));

            var sb = new StringBuilder();
            foreach (var d in data)
            {
                sb.Append(d.ToString("x2"));
            }
            return sb.ToString();
        }
#else
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(str))).ToLowerInvariant();
#endif
    }

    /// <summary>
    /// 计算字符串 UTF-8 字节的 SHA512, 返回小写十六进制串。
    /// </summary>
    /// <param name="str">目标字符串</param>
    public static string ToSha512(this string str)
    {
#if NETSTANDARD2_0 || NETSTANDARD2_1
        using (var sha = SHA512.Create())
        {
            var data = sha.ComputeHash(Encoding.UTF8.GetBytes(str));

            var sb = new StringBuilder();
            foreach (var d in data)
            {
                sb.Append(d.ToString("x2"));
            }
            return sb.ToString();
        }
#else
        return Convert.ToHexString(SHA512.HashData(Encoding.UTF8.GetBytes(str))).ToLowerInvariant();
#endif
    }

    /// <summary>
    /// 把 camelCase 转成 PascalCase(首字母大写)。
    /// </summary>
    /// <param name="str">要转换的字符串</param>
    /// <param name="useCurrentCulture">true 用当前区域文化, 否则用不变文化</param>
    /// <returns>PascalCase 形式的字符串</returns>
    public static string ToPascalCase(this string str, bool useCurrentCulture = false)
    {
        if (string.IsNullOrWhiteSpace(str))
        {
            return str;
        }

        if (str.Length == 1)
        {
            return useCurrentCulture ? str.ToUpper() : str.ToUpperInvariant();
        }

        return (useCurrentCulture ? char.ToUpper(str[0]) : char.ToUpperInvariant(str[0])) + str.Substring(1);
    }

    /// <summary>
    /// 超长时从开头截断到指定长度。
    /// </summary>
    /// <param name="str">目标字符串, 为 null 时返回 null</param>
    /// <param name="maxLength">最大长度</param>
    public static string? Truncate(this string? str, int maxLength)
    {
        if (str == null)
        {
            return null;
        }

        if (str.Length <= maxLength)
        {
            return str;
        }

        return str.Left(maxLength);
    }

    /// <summary>
    /// 超长时只保留末尾指定长度的部分。
    /// </summary>
    /// <param name="str">目标字符串, 为 null 时返回 null</param>
    /// <param name="maxLength">最大长度</param>
    public static string? TruncateFromBeginning(this string? str, int maxLength)
    {
        if (str == null)
        {
            return null;
        }

        if (str.Length <= maxLength)
        {
            return str;
        }

        return str.Right(maxLength);
    }

    /// <summary>
    /// 超长时从开头截断并补上默认后缀 "..."。
    /// 返回的字符串长度不会超过 <paramref name="maxLength"/>。
    /// </summary>
    /// <param name="str">目标字符串, 为 null 时返回 null</param>
    /// <param name="maxLength">最大长度</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="str"/> 为 null 时抛出</exception>
    public static string? TruncateWithPostfix(this string? str, int maxLength)
    {
        return TruncateWithPostfix(str, maxLength, "...");
    }

    /// <summary>
    /// 超长时从开头截断并补上指定后缀, 后缀本身也计入长度。
    /// 返回的字符串长度不会超过 <paramref name="maxLength"/>。
    /// </summary>
    /// <param name="str">目标字符串, 为 null 时返回 null</param>
    /// <param name="maxLength">最大长度</param>
    /// <param name="postfix">截断后补到末尾的后缀</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="str"/> 为 null 时抛出</exception>
    public static string? TruncateWithPostfix(this string? str, int maxLength, string postfix)
    {
        if (str == null)
        {
            return null;
        }

        if (str == string.Empty || maxLength == 0)
        {
            return string.Empty;
        }

        if (str.Length <= maxLength)
        {
            return str;
        }

        if (maxLength <= postfix.Length)
        {
            return postfix.Left(maxLength);
        }

        return str.Left(maxLength - postfix.Length) + postfix;
    }

    /// <summary>
    /// 按 UTF-8 把字符串转成字节数组。
    /// </summary>
    /// <param name="str">目标字符串</param>
    public static byte[] GetBytes(this string str)
    {
        return str.GetBytes(Encoding.UTF8);
    }

    /// <summary>
    /// 按指定编码把字符串转成字节数组。
    /// </summary>
    /// <param name="str">目标字符串</param>
    /// <param name="encoding">要使用的编码</param>
    public static byte[] GetBytes(this string str, Encoding encoding)
    {
        Check.NotNull(str, nameof(str));
        Check.NotNull(encoding, nameof(encoding));

        return encoding.GetBytes(str);
    }

    /// <summary>
    /// 判断字符串里的字母是否全是大写。
    /// </summary>
    /// <param name="input">要判断的字符串</param>
    private static bool IsAllUpperCase(string input)
    {
        for (int i = 0; i < input.Length; i++)
        {
            if (Char.IsLetter(input[i]) && !Char.IsUpper(input[i]))
            {
                return false;
            }
        }

        return true;
    }
}

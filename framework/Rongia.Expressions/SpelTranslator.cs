using System.Text;

namespace Rongia.Expressions;

/// <summary>
/// SpEL 风格表达式到 C# 表达式的翻译器: #变量 转 _v_变量、'文本' 转 "文本"、T(类型)/typeof(类型) 转类型别名占位、文本运算符转符号
/// </summary>
internal static class SpelTranslator
{
    /// <summary>
    /// 变量前缀, 避免与 C# 关键字/类型名冲突
    /// </summary>
    internal const string VariablePrefix = "_v_";

    /// <summary>
    /// 类型占位前缀, 形如 __speltype0__
    /// </summary>
    internal const string TypePlaceholderPrefix = "__speltype";

    private static readonly Dictionary<string, string> WordOperators = new(StringComparer.OrdinalIgnoreCase)
    {
        ["eq"] = "==",
        ["ne"] = "!=",
        ["lt"] = "<",
        ["le"] = "<=",
        ["gt"] = ">",
        ["ge"] = ">=",
        ["and"] = "&&",
        ["or"] = "||",
        ["not"] = "!",
        ["div"] = "/",
        ["mod"] = "%"
    };

    /// <summary>
    /// 翻译表达式
    /// </summary>
    /// <param name="expression">SpEL 风格表达式</param>
    /// <returns>翻译结果</returns>
    public static TranslatedExpression Translate(string expression)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(expression);

        StringBuilder builder = new(expression.Length + 16);
        List<string> typeNames = [];
        int index = 0;

        while (index < expression.Length)
        {
            char current = expression[index];

            if (current == '\'')
            {
                index = ReadSingleQuoted(expression, index, builder);
                continue;
            }

            if (current == '"')
            {
                index = ReadDoubleQuoted(expression, index, builder);
                continue;
            }

            if (current == '#')
            {
                int start = ++index;
                while (index < expression.Length && IsIdentifierPart(expression[index]))
                {
                    index++;
                }

                if (index == start)
                {
                    throw new ExpressionException("表达式中的 # 后缺少变量名。");
                }

                builder.Append(VariablePrefix).Append(expression, start, index - start);
                continue;
            }

            if (current == '@')
            {
                throw new ExpressionException("暂不支持 SpEL 的 @bean 引用。");
            }

            if (current == '?' && index + 1 < expression.Length && (expression[index + 1] == '.' || expression[index + 1] == ':'))
            {
                throw new ExpressionException(expression[index + 1] == '.' ? "暂不支持 SpEL 的安全导航 ?. 运算符。" : "暂不支持 SpEL 的 Elvis 运算符 ?:。");
            }

            if (current == '.' && index + 2 < expression.Length && (expression[index + 1] == '!' || expression[index + 1] == '?') && expression[index + 2] == '[')
            {
                throw new ExpressionException("暂不支持 SpEL 的集合投影/选择运算符 .![] / .?[]。");
            }

            if (IsIdentifierStart(current))
            {
                int start = index;
                while (index < expression.Length && IsIdentifierPart(expression[index]))
                {
                    index++;
                }

                string word = expression[start..index];

                if ((string.Equals(word, "T", StringComparison.Ordinal) || string.Equals(word, "typeof", StringComparison.OrdinalIgnoreCase)) && index < expression.Length && expression[index] == '(')
                {
                    int close = expression.IndexOf(')', index);
                    if (close < 0)
                    {
                        throw new ExpressionException(word + "( 缺少右括号。");
                    }

                    string typeName = expression[(index + 1)..close].Trim();
                    if (typeName.Length == 0)
                    {
                        throw new ExpressionException(word + "( 内缺少类型名。");
                    }

                    if (typeName.Contains('<', StringComparison.Ordinal))
                    {
                        throw new ExpressionException("暂不支持泛型类型 " + typeName + "。");
                    }

                    builder.Append(TypePlaceholderPrefix).Append(typeNames.Count).Append("__");
                    typeNames.Add(typeName);
                    index = close + 1;
                    continue;
                }

                if (WordOperators.TryGetValue(word, out string? symbol))
                {
                    builder.Append(' ').Append(symbol).Append(' ');
                    continue;
                }

                builder.Append(word);
                continue;
            }

            builder.Append(current);
            index++;
        }

        return new TranslatedExpression(builder.ToString(), typeNames);
    }

    private static bool IsIdentifierStart(char value) => char.IsLetter(value) || value == '_';

    private static bool IsIdentifierPart(char value) => char.IsLetterOrDigit(value) || value == '_';

    private static int ReadSingleQuoted(string source, int index, StringBuilder builder)
    {
        index++;
        builder.Append('"');

        while (index < source.Length)
        {
            char current = source[index];

            if (current == '\'')
            {
                if (index + 1 < source.Length && source[index + 1] == '\'')
                {
                    builder.Append('\'');
                    index += 2;
                    continue;
                }

                builder.Append('"');

                return index + 1;
            }

            if (current == '\\' && index + 1 < source.Length)
            {
                char next = source[index + 1];
                builder.Append(next switch
                {
                    'n' => "\\n",
                    't' => "\\t",
                    'r' => "\\r",
                    '"' => "\\\"",
                    '\\' => "\\\\",
                    _ => next.ToString()
                });
                index += 2;
                continue;
            }

            if (current == '"')
            {
                builder.Append("\\\"");
            }
            else if (current == '\\')
            {
                builder.Append("\\\\");
            }
            else
            {
                builder.Append(current);
            }

            index++;
        }

        throw new ExpressionException("字符串字面量未闭合。");
    }

    private static int ReadDoubleQuoted(string source, int index, StringBuilder builder)
    {
        builder.Append(source[index]);
        index++;

        while (index < source.Length)
        {
            char current = source[index];
            builder.Append(current);

            if (current == '\\' && index + 1 < source.Length)
            {
                builder.Append(source[index + 1]);
                index += 2;
                continue;
            }

            index++;

            if (current == '"')
            {
                return index;
            }
        }

        throw new ExpressionException("字符串字面量未闭合。");
    }
}

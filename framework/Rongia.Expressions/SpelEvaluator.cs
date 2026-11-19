using System.Collections.Concurrent;
using System.Reflection;
using DynamicExpresso;

namespace Rongia.Expressions;

/// <summary>
/// 表达式求值器: 把翻译结果中的类型占位替换为已注册的类型别名, 并按「文本 + 参数签名」缓存已解析的 Lambda
/// </summary>
internal static class SpelEvaluator
{
    private static readonly Interpreter Engine = new(InterpreterOptions.Default | InterpreterOptions.LambdaExpressions | InterpreterOptions.CaseInsensitive);

    private static readonly ConcurrentDictionary<string, Lambda> LambdaCache = new(StringComparer.Ordinal);

    private static readonly ConcurrentDictionary<Type, string> TypeAliases = new();

    private static readonly Dictionary<string, Type> BuiltInTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["string"] = typeof(string),
        ["int"] = typeof(int),
        ["long"] = typeof(long),
        ["short"] = typeof(short),
        ["byte"] = typeof(byte),
        ["bool"] = typeof(bool),
        ["decimal"] = typeof(decimal),
        ["double"] = typeof(double),
        ["float"] = typeof(float),
        ["char"] = typeof(char),
        ["object"] = typeof(object),
        ["DateTime"] = typeof(DateTime),
        ["DateTimeOffset"] = typeof(DateTimeOffset),
        ["TimeSpan"] = typeof(TimeSpan),
        ["Guid"] = typeof(Guid),
        ["Math"] = typeof(Math),
        ["Convert"] = typeof(Convert),
        ["Enumerable"] = typeof(Enumerable)
    };

    private static readonly object ParseGate = new();

    private static readonly object ReferenceGate = new();

    /// <summary>
    /// 预处理: 翻译表达式并注册其中引用的类型, 返回可直接解析的 C# 文本
    /// </summary>
    /// <param name="source">原始表达式</param>
    /// <returns>C# 表达式文本</returns>
    public static string Prepare(string source)
    {
        TranslatedExpression translated = SpelTranslator.Translate(source);

        if (translated.TypeNames.Count == 0)
        {
            return translated.Text;
        }

        string text = translated.Text;

        for (int index = 0; index < translated.TypeNames.Count; index++)
        {
            string alias = EnsureTypeAlias(translated.TypeNames[index], source);
            text = text.Replace(SpelTranslator.TypePlaceholderPrefix + index + "__", alias, StringComparison.Ordinal);
        }

        return text;
    }

    /// <summary>
    /// 执行预处理后的表达式
    /// </summary>
    /// <param name="translated">预处理后的 C# 表达式</param>
    /// <param name="source">原始表达式(用于错误信息)</param>
    /// <param name="context">求值上下文</param>
    /// <returns>求值结果</returns>
    public static object? Evaluate(string translated, string source, ExpressionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        List<(string Name, Type Type, object? Value)> slots = new(context.Variables.Count);

        foreach (KeyValuePair<string, object?> pair in context.Variables)
        {
            slots.Add((SpelTranslator.VariablePrefix + pair.Key, pair.Value?.GetType() ?? typeof(object), pair.Value));
        }

        slots.Sort(static (left, right) => string.CompareOrdinal(left.Name, right.Name));

        string cacheKey = translated + "\u0001" + string.Join("|", slots.Select(static slot => slot.Name + ":" + slot.Type.FullName));

        if (!LambdaCache.TryGetValue(cacheKey, out Lambda? lambda))
        {
            Parameter[] parameters = slots.Select(static slot => new Parameter(slot.Name, slot.Type)).ToArray();

            lock (ParseGate)
            {
                lambda = LambdaCache.GetOrAdd(cacheKey, _ => Parse(translated, source, parameters));
            }
        }

        object?[] values = slots.Select(static slot => slot.Value).ToArray();

        try
        {
            return lambda.Invoke(values);
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            throw new ExpressionException("表达式调用目标方法时抛出异常: " + exception.InnerException.Message + " | 表达式: " + source, exception.InnerException);
        }
        catch (ExpressionException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new ExpressionException("表达式求值失败: " + exception.Message + " | 表达式: " + source, exception);
        }
    }

    private static string EnsureTypeAlias(string typeName, string source)
    {
        Type type = ResolveType(typeName) ?? throw new ExpressionException("无法解析类型 " + typeName + ", 请使用可见的全名(如 System.String)。| 表达式: " + source);

        return TypeAliases.GetOrAdd(type, static resolved =>
        {
            string alias = "_spel_" + resolved.FullName!.Replace('.', '_').Replace('+', '_').Replace('`', '_');

            lock (ReferenceGate)
            {
                Engine.Reference(resolved, alias);
            }

            return alias;
        });
    }

    private static Type? ResolveType(string typeName)
    {
        if (BuiltInTypes.TryGetValue(typeName, out Type? builtIn))
        {
            return builtIn;
        }

        Type? type = Type.GetType(typeName, false) ?? Type.GetType("System." + typeName, false);

        if (type is not null)
        {
            return type;
        }

        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            type = assembly.GetType(typeName, false);

            if (type is not null)
            {
                return type;
            }
        }

        return null;
    }

    private static Lambda Parse(string translated, string source, Parameter[] parameters)
    {
        try
        {
            return Engine.Parse(translated, typeof(object), parameters);
        }
        catch (ExpressionException)
        {
            throw;
        }
        catch (Exception exception)
        {
            string message = exception.Message.Replace(SpelTranslator.VariablePrefix, "#", StringComparison.Ordinal);

            throw new ExpressionException("表达式解析失败: " + message + " | 表达式: " + source, exception);
        }
    }
}

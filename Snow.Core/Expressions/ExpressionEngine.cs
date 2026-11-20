using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using DynamicExpresso;

namespace Snow.Expressions;

/// <summary>
/// 表达式引擎入口: 表达式按 C# 语法书写(变量名即参数名, 另有 root / p0 / a0 / result 等约定变量),
/// 底层由 DynamicExpresso 求值, 解析结果按「文本 + 变量签名」缓存, 反射只在首次解析时发生
/// </summary>
public static class ExpressionEngine
{
    private static readonly Interpreter Engine = new(InterpreterOptions.Default | InterpreterOptions.LambdaExpressions | InterpreterOptions.CaseInsensitive);

    private static readonly ConcurrentDictionary<string, CompiledExpression> Cache = new(StringComparer.Ordinal);

    private static readonly ConcurrentDictionary<string, Lambda> LambdaCache = new(StringComparer.Ordinal);

    private static readonly object ParseGate = new();

    /// <summary>
    /// 解析表达式(带缓存)
    /// </summary>
    /// <param name="expression">表达式文本</param>
    /// <returns>已解析表达式</returns>
    public static CompiledExpression Parse(string expression)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(expression);

        return Cache.GetOrAdd(expression, static text => new CompiledExpression(text));
    }

    /// <summary>
    /// 求值
    /// </summary>
    /// <param name="expression">表达式文本</param>
    /// <param name="context">求值上下文</param>
    /// <returns>求值结果</returns>
    public static object? Evaluate(string expression, ExpressionContext context)
    {
        return Parse(expression).Evaluate(context);
    }

    /// <summary>
    /// 求值并转换为布尔
    /// </summary>
    /// <param name="expression">表达式文本</param>
    /// <param name="context">求值上下文</param>
    /// <returns>布尔结果</returns>
    public static bool EvaluateBoolean(string expression, ExpressionContext context)
    {
        return ToBoolean(Evaluate(expression, context));
    }

    /// <summary>
    /// 求值并转换为字符串
    /// </summary>
    /// <param name="expression">表达式文本</param>
    /// <param name="context">求值上下文</param>
    /// <returns>字符串结果</returns>
    public static string EvaluateString(string expression, ExpressionContext context)
    {
        return ToInvariantString(Evaluate(expression, context));
    }

    /// <summary>
    /// 执行已解析表达式的核心求值: 按上下文变量构造参数, 命中缓存直接调用, 未命中先解析
    /// </summary>
    /// <param name="expression">表达式文本</param>
    /// <param name="context">求值上下文</param>
    /// <returns>求值结果</returns>
    internal static object? EvaluateCore(string expression, ExpressionContext context)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(expression);
        ArgumentNullException.ThrowIfNull(context);

        List<(string Name, Type Type, object? Value)> slots = new(context.Variables.Count);

        foreach (KeyValuePair<string, object?> pair in context.Variables)
        {
            slots.Add((pair.Key, pair.Value?.GetType() ?? typeof(object), pair.Value));
        }

        slots.Sort(static (left, right) => string.CompareOrdinal(left.Name, right.Name));

        string cacheKey = expression + "\u0001" + string.Join("|", slots.Select(static slot => slot.Name + ":" + slot.Type.FullName));

        if (!LambdaCache.TryGetValue(cacheKey, out Lambda? lambda))
        {
            Parameter[] parameters = slots.Select(static slot => new Parameter(slot.Name, slot.Type)).ToArray();

            lock (ParseGate)
            {
                lambda = LambdaCache.GetOrAdd(cacheKey, _ => ParseLambda(expression, parameters));
            }
        }

        object?[] values = slots.Select(static slot => slot.Value).ToArray();

        try
        {
            return lambda.Invoke(values);
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            throw new ExpressionException("表达式调用目标方法时抛出异常: " + exception.InnerException.Message + " | 表达式: " + expression, exception.InnerException);
        }
        catch (ExpressionException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new ExpressionException("表达式求值失败: " + exception.Message + " | 表达式: " + expression, exception);
        }
    }

    /// <summary>
    /// 解析为可重复调用的 Lambda
    /// </summary>
    /// <param name="expression">表达式文本</param>
    /// <param name="parameters">上下文变量声明的参数</param>
    /// <returns>已编译 Lambda</returns>
    private static Lambda ParseLambda(string expression, Parameter[] parameters)
    {
        try
        {
            return Engine.Parse(expression, typeof(object), parameters);
        }
        catch (ExpressionException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new ExpressionException("表达式解析失败: " + exception.Message + " | 表达式: " + expression, exception);
        }
    }

    /// <summary>
    /// 转布尔: null/false 为假, 字符串按 bool.TryParse
    /// </summary>
    /// <param name="value">待转换值</param>
    /// <returns>布尔结果</returns>
    private static bool ToBoolean(object? value) => value switch
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
    private static string ToInvariantString(object? value) => value switch
    {
        null => string.Empty,
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? string.Empty
    };
}

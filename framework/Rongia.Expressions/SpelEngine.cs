using System.Collections.Concurrent;

namespace Rongia.Expressions;

/// <summary>
/// 表达式引擎入口: 兼容 Spring SpEL 常用语法, 底层由 DynamicExpresso 求值, 解析结果按表达式文本缓存
/// </summary>
public static class SpelEngine
{
    private static readonly ConcurrentDictionary<string, SpelExpression> Cache = new(StringComparer.Ordinal);

    /// <summary>
    /// 解析表达式(带缓存)
    /// </summary>
    /// <param name="expression">表达式文本</param>
    /// <returns>已解析表达式</returns>
    public static SpelExpression Parse(string expression)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(expression);

        return Cache.GetOrAdd(expression, static text => new SpelExpression(text, SpelEvaluator.Prepare(text)));
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
        return SpelValue.ToBoolean(Evaluate(expression, context));
    }

    /// <summary>
    /// 求值并转换为字符串
    /// </summary>
    /// <param name="expression">表达式文本</param>
    /// <param name="context">求值上下文</param>
    /// <returns>字符串结果</returns>
    public static string EvaluateString(string expression, ExpressionContext context)
    {
        return SpelValue.ToInvariantString(Evaluate(expression, context));
    }
}

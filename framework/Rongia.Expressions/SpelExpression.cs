namespace Rongia.Expressions;

/// <summary>
/// 已解析的表达式: 保存原文与预处理后的 C# 文本, 求值委托给 DynamicExpresso
/// </summary>
public sealed class SpelExpression
{
    private readonly string _translated;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="source">原始表达式文本</param>
    /// <param name="translated">预处理(翻译 + 类型别名注册)后的 C# 表达式文本</param>
    internal SpelExpression(string source, string translated)
    {
        Source = source ?? throw new ArgumentNullException(nameof(source));
        _translated = translated ?? throw new ArgumentNullException(nameof(translated));
    }

    /// <summary>
    /// 原始表达式文本
    /// </summary>
    public string Source { get; }

    /// <summary>
    /// 求值
    /// </summary>
    /// <param name="context">求值上下文</param>
    /// <returns>求值结果</returns>
    public object? Evaluate(ExpressionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return SpelEvaluator.Evaluate(_translated, Source, context);
    }
}

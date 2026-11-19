namespace Rongia.Expressions;

/// <summary>
/// 已解析的表达式: 保存原文, 求值委托给 <see cref="ExpressionEngine"/>
/// </summary>
public sealed class CompiledExpression
{
    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="source">表达式文本(C# 语法)</param>
    internal CompiledExpression(string source)
    {
        Source = source ?? throw new ArgumentNullException(nameof(source));
    }

    /// <summary>
    /// 表达式文本
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

        return ExpressionEngine.EvaluateCore(Source, context);
    }
}

using Snow.Aop;

namespace Snow.Expressions;

/// <summary>
/// 从拦截调用上下文构造表达式上下文: 统一「root + 参数名 / p{i} / a{i} / result」的变量约定, 供各拦截器注解复用
/// </summary>
public static class ExpressionContextFactory
{
    /// <summary>
    /// 构造表达式上下文(不带 result, 用于入参类表达式)
    /// </summary>
    /// <param name="context">方法调用上下文</param>
    /// <returns>表达式上下文</returns>
    public static ExpressionContext Create(AspectContext context)
    {
        return Create(context, null, false);
    }

    /// <summary>
    /// 构造表达式上下文: 塞入参数名 / p{i} / a{i} 与 root, 需要时附带 result
    /// </summary>
    /// <param name="context">方法调用上下文</param>
    /// <param name="result">目标方法的返回值</param>
    /// <param name="hasResult">是否暴露 result 变量</param>
    /// <returns>表达式上下文</returns>
    public static ExpressionContext Create(AspectContext context, object? result, bool hasResult)
    {
        ArgumentNullException.ThrowIfNull(context);

        ExpressionContext expressionContext = new(new ExpressionRoot(context.Method.Name, context.Method, context.TargetObject, context.Arguments));

        for (int index = 0; index < context.Arguments.Length; index++)
        {
            expressionContext.SetVariable("p" + index, context.Arguments[index]);
            expressionContext.SetVariable("a" + index, context.Arguments[index]);
        }

        foreach (KeyValuePair<string, object?> pair in context.ArgumentsDictionary)
        {
            expressionContext.SetVariable(pair.Key, pair.Value);
        }

        if (hasResult)
        {
            expressionContext.SetVariable("result", result);
        }

        return expressionContext;
    }
}
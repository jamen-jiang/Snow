using Snow.DynamicProxy;

namespace Snow.Expressions;

/// <summary>
/// 从拦截调用上下文构造表达式上下文: 统一「root + 参数名 / p{i} / a{i} / result」的变量约定, 供各拦截器复用
/// </summary>
public static class ExpressionContextFactory
{
    /// <summary>
    /// 构造表达式上下文(不带 result, 用于入参类表达式)
    /// </summary>
    /// <param name="invocation">方法调用</param>
    /// <returns>表达式上下文</returns>
    public static ExpressionContext Create(ISnowMethodInvocation invocation)
    {
        return Create(invocation, null, false);
    }

    /// <summary>
    /// 构造表达式上下文: 塞入参数名 / p{i} / a{i} 与 root, 需要时附带 result
    /// </summary>
    /// <param name="invocation">方法调用</param>
    /// <param name="result">目标方法的返回值</param>
    /// <param name="hasResult">是否暴露 result 变量</param>
    /// <returns>表达式上下文</returns>
    public static ExpressionContext Create(ISnowMethodInvocation invocation, object? result, bool hasResult)
    {
        ArgumentNullException.ThrowIfNull(invocation);

        ExpressionContext expressionContext = new(new ExpressionRoot(invocation.Method.Name, invocation.Method, invocation.TargetObject, invocation.Arguments));

        for (int index = 0; index < invocation.Arguments.Length; index++)
        {
            expressionContext.SetVariable("p" + index, invocation.Arguments[index]);
            expressionContext.SetVariable("a" + index, invocation.Arguments[index]);
        }

        foreach (KeyValuePair<string, object?> pair in invocation.ArgumentsDictionary)
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

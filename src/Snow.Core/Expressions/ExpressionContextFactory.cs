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
    /// <param name="invocation">方法调用上下文</param>
    /// <returns>表达式上下文</returns>
    public static ExpressionContext Create(AopMethodInvocation invocation)
    {
        return Create(invocation, null, false);
    }

    /// <summary>
    /// 构造表达式上下文: 塞入参数名 / p{i} / a{i} 与 root, 需要时附带 result
    /// </summary>
    /// <param name="invocation">方法调用上下文</param>
    /// <param name="result">目标方法的返回值</param>
    /// <param name="hasResult">是否暴露 result 变量</param>
    /// <returns>表达式上下文</returns>
    public static ExpressionContext Create(AopMethodInvocation invocation, object? result, bool hasResult)
    {
        ArgumentNullException.ThrowIfNull(invocation);

        ExpressionContext context = new(new ExpressionRoot(invocation.Method.Name, invocation.Method, invocation.Target, invocation.Arguments));

        for (int index = 0; index < invocation.Arguments.Length; index++)
        {
            context.SetVariable("p" + index, invocation.Arguments[index]);
            context.SetVariable("a" + index, invocation.Arguments[index]);
        }

        foreach (KeyValuePair<string, object?> pair in invocation.ArgumentsDictionary)
        {
            context.SetVariable(pair.Key, pair.Value);
        }

        if (hasResult)
        {
            context.SetVariable("result", result);
        }

        return context;
    }
}

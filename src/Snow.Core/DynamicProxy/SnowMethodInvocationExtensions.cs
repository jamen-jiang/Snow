namespace Snow.DynamicProxy;

/// <summary>
/// <see cref="ISnowMethodInvocation"/> 的便捷扩展
/// </summary>
public static class SnowMethodInvocationExtensions
{
    /// <summary>
    /// 取被拦截方法签名里的 <see cref="CancellationToken"/> 实参; 没有就返回 <see cref="CancellationToken.None"/>
    /// </summary>
    /// <param name="invocation">方法调用</param>
    /// <returns>取消令牌</returns>
    public static CancellationToken GetCancellationToken(this ISnowMethodInvocation invocation)
    {
        ArgumentNullException.ThrowIfNull(invocation);

        foreach (object? argument in invocation.Arguments)
        {
            if (argument is CancellationToken cancellationToken)
            {
                return cancellationToken;
            }
        }

        return CancellationToken.None;
    }
}

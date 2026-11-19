using System.Reflection;

namespace Rongia.Expressions;

/// <summary>
/// 表达式中的 root 对象: 提供被拦截方法的元信息
/// </summary>
public sealed class ExpressionRoot
{
    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="methodName">方法名</param>
    /// <param name="method">方法元数据</param>
    /// <param name="target">目标对象</param>
    /// <param name="args">实参</param>
    public ExpressionRoot(string methodName, MethodInfo? method = null, object? target = null, object?[]? args = null)
    {
        MethodName = methodName ?? throw new ArgumentNullException(nameof(methodName));
        Method = method;
        Target = target;
        Args = args ?? [];
    }

    /// <summary>
    /// 方法名
    /// </summary>
    public string MethodName { get; }

    /// <summary>
    /// 方法元数据
    /// </summary>
    public MethodInfo? Method { get; }

    /// <summary>
    /// 目标对象
    /// </summary>
    public object? Target { get; }

    /// <summary>
    /// 实参
    /// </summary>
    public object?[] Args { get; }

    /// <summary>
    /// 目标对象类型名
    /// </summary>
    public string? TargetTypeName => Target?.GetType().FullName ?? Method?.DeclaringType?.FullName;
}

using System.Reflection;
using Castle.DynamicProxy;

namespace Snow.Aop.Castle;

/// <summary>
/// <see cref="AspectContext"/> 的默认实现: 直接包住 Castle 的 <see cref="IInvocation"/>, 实参数组不复制, 原地改动直接落到调用上。
/// <para>由 <see cref="AopInterceptorAdapter"/> 每次调用新建一个, 整条链共享; 需要换上下文形状时提供自己的
/// <see cref="AspectContext"/> 实现即可, 拦截器无感。</para>
/// </summary>
public sealed class DefaultAspectContext : AspectContext
{
    private readonly IInvocation _invocation;
    private readonly Lazy<IReadOnlyDictionary<string, object?>> _argumentsDictionary;

    /// <summary>
    /// 创建调用上下文
    /// </summary>
    /// <param name="invocation">Castle 的调用信息</param>
    /// <param name="serviceProvider">服务提供者</param>
    public DefaultAspectContext(IInvocation invocation, IServiceProvider serviceProvider)
    {
        _invocation = invocation ?? throw new ArgumentNullException(nameof(invocation));
        ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _argumentsDictionary = new Lazy<IReadOnlyDictionary<string, object?>>(BuildArgumentsDictionary);
    }

    /// <inheritdoc />
    public override object?[] Arguments => _invocation.Arguments;

    /// <inheritdoc />
    public override IReadOnlyDictionary<string, object?> ArgumentsDictionary => _argumentsDictionary.Value;

    /// <inheritdoc />
    public override Type[]? GenericArguments => Method.IsGenericMethod ? Method.GetGenericArguments() : null;

    /// <inheritdoc />
    public override object? TargetObject => _invocation.InvocationTarget ?? _invocation.MethodInvocationTarget;

    /// <inheritdoc />
    public override MethodInfo Method => _invocation.MethodInvocationTarget ?? _invocation.Method;

    /// <inheritdoc />
    public override object? ReturnValue { get; set; }

    /// <inheritdoc />
    public override IServiceProvider ServiceProvider { get; }

    private IReadOnlyDictionary<string, object?> BuildArgumentsDictionary()
    {
        ParameterInfo[] parameters = Method.GetParameters();
        Dictionary<string, object?> dictionary = new(parameters.Length, StringComparer.Ordinal);

        for (int index = 0; index < parameters.Length && index < Arguments.Length; index++)
        {
            dictionary[parameters[index].Name ?? "arg" + index] = Arguments[index];
        }

        return dictionary;
    }
}

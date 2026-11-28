using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using Snow.DynamicProxy;

namespace Snow.Castle.DynamicProxy;

/// <summary>
/// <see cref="ISnowMethodInvocation"/> 在 Castle <see cref="IInvocation"/> 上的适配基类, 与 ABP 的 <c>CastleAbpMethodInvocationAdapterBase</c> 同形。
/// </summary>
public abstract class CastleSnowMethodInvocationAdapterBase : ISnowMethodInvocation
{
    /// <inheritdoc />
    public object?[] Arguments => Invocation.Arguments;

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object?> ArgumentsDictionary => _lazyArgumentsDictionary.Value;

    private readonly Lazy<IReadOnlyDictionary<string, object?>> _lazyArgumentsDictionary;

    /// <inheritdoc />
    public Type[]? GenericArguments => Invocation.GenericArguments;

    /// <inheritdoc />
    public object? TargetObject => Invocation.InvocationTarget ?? Invocation.MethodInvocationTarget;

    /// <inheritdoc />
    public MethodInfo Method => Invocation.MethodInvocationTarget ?? Invocation.Method;

    /// <inheritdoc />
    /// <remarks>
    /// 同步返回值会同时写回 Castle 的调用信息: Castle 的异步拦截基类对同步方法会丢弃泛型拦截结果,
    /// 只认 <see cref="IInvocation.ReturnValue"/>, 不同步写回的话短路返回的值到不了调用方。
    /// </remarks>
    public object? ReturnValue
    {
        get => _returnValue;
        set
        {
            _returnValue = value;

            if (IsSynchronousReturnType)
            {
                Invocation.ReturnValue = value;
            }
        }
    }

    private object? _returnValue;

    private bool IsSynchronousReturnType
    {
        get
        {
            Type returnType = Invocation.Method.ReturnType;

            if (typeof(Task).IsAssignableFrom(returnType) || returnType == typeof(ValueTask))
            {
                return false;
            }

            return !(returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(ValueTask<>));
        }
    }

    /// <summary>
    /// Castle 的调用信息
    /// </summary>
    protected IInvocation Invocation { get; }

    /// <summary>
    /// 创建适配器
    /// </summary>
    /// <param name="invocation">Castle 的调用信息</param>
    protected CastleSnowMethodInvocationAdapterBase(IInvocation invocation)
    {
        Invocation = invocation;
        _lazyArgumentsDictionary = new Lazy<IReadOnlyDictionary<string, object?>>(GetArgumentsDictionary);
    }

    /// <inheritdoc />
    public abstract Task ProceedAsync();

    /// <summary>
    /// 按参数名把实参整理成只读字典(懒建, 第一次访问 <see cref="ArgumentsDictionary"/> 时才执行)
    /// </summary>
    /// <returns>参数名到实参的只读字典</returns>
    private IReadOnlyDictionary<string, object?> GetArgumentsDictionary()
    {
        Dictionary<string, object?> dictionary = new();

        ParameterInfo[] methodParameters = Method.GetParameters();
        for (int index = 0; index < methodParameters.Length; index++)
        {
            dictionary[methodParameters[index].Name!] = Invocation.Arguments[index];
        }

        return dictionary;
    }
}

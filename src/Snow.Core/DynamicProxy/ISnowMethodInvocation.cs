using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace Snow.DynamicProxy;

/// <summary>
/// 一次被拦截调用的信息, 整条拦截链共享: 实参的原地修改与 <see cref="ReturnValue"/> 都落回真实调用。
/// </summary>
public interface ISnowMethodInvocation
{
    /// <summary>
    /// 实参数组, 可原地修改
    /// </summary>
    object?[] Arguments { get; }

    /// <summary>
    /// 按参数名索引实参的只读视图
    /// </summary>
    IReadOnlyDictionary<string, object?> ArgumentsDictionary { get; }

    /// <summary>
    /// 泛型方法的类型实参; 非泛型方法为 <c>null</c>
    /// </summary>
    Type[]? GenericArguments { get; }

    /// <summary>
    /// 被代理的目标对象
    /// </summary>
    object? TargetObject { get; }

    /// <summary>
    /// 被调用的方法
    /// </summary>
    MethodInfo Method { get; }

    /// <summary>
    /// 目标方法的返回值: 拦截器可读取以包装结果, 也可直接赋值以实现短路
    /// </summary>
    object? ReturnValue { get; set; }

    /// <summary>
    /// 执行目标方法(或交给下一环拦截器)
    /// </summary>
    /// <returns>表示执行过程的任务</returns>
    Task ProceedAsync();
}

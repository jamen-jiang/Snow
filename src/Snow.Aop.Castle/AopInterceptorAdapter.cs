using System.Collections.Concurrent;
using System.Reflection;
using Castle.DynamicProxy;

namespace Snow.Aop.Castle;

/// <summary>
/// 把 <see cref="InterceptorAttribute"/> 链适配成 Castle 拦截器: 按「类级特性 + 实现方法上的特性」组装责任链,
/// 并负责同步返回值、<see cref="Task"/>、<see cref="Task{TResult}"/>、<see cref="ValueTask"/>、<see cref="ValueTask{TResult}"/>
/// 五种返回形态的收尾。
/// <para>异步方法(返回 Task / Task&lt;T&gt; / ValueTask / ValueTask&lt;T&gt;)只把拦截链包成对应的异步结果交回, 不在拦截点阻塞调用线程;
/// 同步方法因为 Castle 的拦截点是同步的, 仍需当场拿结果。</para>
/// </summary>
internal sealed class AopInterceptorAdapter : IInterceptor
{
    private readonly Type _implementationType;
    private readonly InterceptorAttribute[] _classInterceptors;
    private readonly IServiceProvider _serviceProvider;
    private static readonly MethodInfo AwaitChainMethod =
        typeof(AopInterceptorAdapter).GetMethod(nameof(AwaitChainAsync), BindingFlags.NonPublic | BindingFlags.Static)!;

    private readonly ConcurrentDictionary<MethodInfo, InterceptorAttribute[]> _interceptorsByMethod = new();

    /// <summary>
    /// 创建适配器
    /// </summary>
    /// <param name="implementationType">实现类型, 用于按方法名与参数类型找实现方法上的特性</param>
    /// <param name="classInterceptors">类级拦截器</param>
    /// <param name="serviceProvider">服务提供者</param>
    public AopInterceptorAdapter(Type implementationType, InterceptorAttribute[] classInterceptors, IServiceProvider serviceProvider)
    {
        _implementationType = implementationType;
        _classInterceptors = classInterceptors;
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public void Intercept(IInvocation invocation)
    {
        MethodInfo method = invocation.MethodInvocationTarget ?? invocation.Method;

        AopMethodInvocation methodInvocation = new(
            invocation.InvocationTarget ?? invocation.MethodInvocationTarget,
            method,
            invocation.Arguments,
            _serviceProvider,
            proceed: () => Task.CompletedTask);

        InterceptorAttribute[] interceptors = ResolveInterceptors(method);

        Task Invoke(int index)
        {
            if (index >= interceptors.Length)
            {
                return ExecuteAsync();
            }

            methodInvocation.SetProceed(() => Invoke(index + 1));

            return interceptors[index].InvokeAsync(methodInvocation);
        }

        async Task ExecuteAsync()
        {
            methodInvocation.ReturnValue = await ExecuteTargetAsync(invocation).ConfigureAwait(false);
        }

        Task chain = Invoke(0);
        Type returnType = method.ReturnType;

        if (!IsAsyncReturnType(returnType))
        {
            // 同步方法的拦截点本身是同步的, 只能在当场阻塞拿结果
            chain.GetAwaiter().GetResult();
            invocation.ReturnValue = methodInvocation.ReturnValue;

            return;
        }

        // 异步方法: 不阻塞调用线程, 直接把拦截链包成与声明返回类型一致的异步结果交回去,
        // 调用方 await 时才会推进目标方法(这才是真正的异步拦截; 旧实现会在此 GetAwaiter().GetResult() 卡线程)
        invocation.ReturnValue = CreateAsyncResult(returnType, chain, methodInvocation);
    }

    private InterceptorAttribute[] ResolveInterceptors(MethodInfo method)
    {
        return _interceptorsByMethod.GetOrAdd(method, static (key, state) =>
        {
            List<InterceptorAttribute> interceptors = new(state.ClassInterceptors);

            MethodInfo? implementationMethod = state.ImplementationType.GetMethod(
                key.Name,
                BindingFlags.Public | BindingFlags.Instance,
                binder: null,
                key.GetParameters().Select(parameter => parameter.ParameterType).ToArray(),
                modifiers: null);

            if (implementationMethod is not null)
            {
                interceptors.AddRange(implementationMethod.GetCustomAttributes<InterceptorAttribute>(true));
            }

            return interceptors.ToArray();
        }, (ImplementationType: _implementationType, ClassInterceptors: _classInterceptors));
    }

    private static bool IsAsyncReturnType(Type returnType)
    {
        return typeof(Task).IsAssignableFrom(returnType)
            || returnType == typeof(ValueTask)
            || (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(ValueTask<>));
    }

    /// <summary>
    /// 按声明返回类型把拦截链包装成异步结果: 链条完成(含目标方法与后置拦截器)后才算完成, 结果取上下文里的返回值
    /// </summary>
    /// <param name="returnType">声明返回类型</param>
    /// <param name="chain">拦截链任务</param>
    /// <param name="methodInvocation">调用上下文</param>
    /// <returns>与声明返回类型一致的异步结果</returns>
    private static object CreateAsyncResult(Type returnType, Task chain, AopMethodInvocation methodInvocation)
    {
        if (returnType == typeof(Task))
        {
            return chain;
        }

        if (returnType == typeof(ValueTask))
        {
            return new ValueTask(chain);
        }

        Type resultType = returnType.GetGenericArguments()[0];
        object taskOfResult = AwaitChainMethod.MakeGenericMethod(resultType).Invoke(null, [chain, methodInvocation])!;

        if (returnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            return taskOfResult;
        }

        // ValueTask<T> 由 Task<T> 构造
        return Activator.CreateInstance(returnType, taskOfResult)!;
    }

    /// <summary>
    /// 等待拦截链并把上下文里的返回值转成声明结果类型
    /// </summary>
    /// <typeparam name="TResult">声明返回类型</typeparam>
    /// <param name="chain">拦截链任务</param>
    /// <param name="methodInvocation">调用上下文</param>
    /// <returns>拦截结果</returns>
    private static async Task<TResult> AwaitChainAsync<TResult>(Task chain, AopMethodInvocation methodInvocation)
    {
        await chain.ConfigureAwait(false);

        if (methodInvocation.ReturnValue is TResult result)
        {
            return result;
        }

        if (methodInvocation.ReturnValue is null)
        {
            // 短路且未给返回值时按声明类型给默认值(值类型不能返回 null)
            return default!;
        }

        throw new InvalidCastException(
            "拦截器给出的返回值类型与声明返回类型不符: 声明 "
                + typeof(TResult).FullName
                + ", 实际 "
                + methodInvocation.ReturnValue.GetType().FullName
                + "。"
        );
    }

    private static async Task<object?> ExecuteTargetAsync(IInvocation invocation)
    {
        invocation.Proceed();

        object? returned = invocation.ReturnValue;

        switch (returned)
        {
            case null:
                return null;

            case Task task:
            {
                await task.ConfigureAwait(false);

                // 用声明类型判断: async Task 的运行时类型其实是泛型的 Task<VoidTaskResult>
                return invocation.Method.ReturnType.IsGenericType
                    ? task.GetType().GetProperty("Result")!.GetValue(task)
                    : null;
            }

            case ValueTask valueTask:
                await valueTask.ConfigureAwait(false);
                return null;
        }

        // ValueTask<T> 是结构体, 装箱后只能走 AsTask 取结果
        Type returnedType = returned.GetType();

        if (returnedType.IsGenericType && returnedType.GetGenericTypeDefinition() == typeof(ValueTask<>))
        {
            Task asTask = (Task)returnedType.GetMethod("AsTask")!.Invoke(returned, null)!;

            await asTask.ConfigureAwait(false);

            return asTask.GetType().GetProperty("Result")!.GetValue(asTask);
        }

        return returned;
    }
}

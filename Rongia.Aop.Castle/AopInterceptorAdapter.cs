using System.Collections.Concurrent;
using System.Reflection;
using Castle.DynamicProxy;

namespace Rongia.Aop.Castle;

/// <summary>
/// 把 <see cref="InterceptorAttribute"/> 链适配成 Castle 拦截器: 按「类级特性 + 实现方法上的特性」组装责任链,
/// 并负责同步返回值、<see cref="Task"/>、<see cref="Task{TResult}"/>、<see cref="ValueTask"/>、<see cref="ValueTask{TResult}"/>
/// 五种返回形态的收尾。
/// </summary>
internal sealed class AopInterceptorAdapter : IInterceptor
{
    private readonly Type _implementationType;
    private readonly InterceptorAttribute[] _classInterceptors;
    private readonly IServiceProvider _serviceProvider;
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
        bool proceeded = false;

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
            proceeded = true;
            methodInvocation.ReturnValue = await ExecuteTargetAsync(invocation).ConfigureAwait(false);
        }

        Invoke(0).GetAwaiter().GetResult();

        if (IsAsyncReturnType(method.ReturnType))
        {
            // 异步方法: 正常执行时 Castle 的 ReturnValue 已是目标的 Task/ValueTask(调用方 await 它), 不能被解包结果覆盖;
            // 短路时链条没走到目标, 需要按声明返回类型把上下文返回值包成已完成的 Task/ValueTask
            if (!proceeded)
            {
                invocation.ReturnValue = CreateCompletedResult(method.ReturnType, methodInvocation.ReturnValue);
            }
        }
        else
        {
            // 同步方法(含短路)必须回写, 否则值类型返回会抛"未设置返回值"
            invocation.ReturnValue = methodInvocation.ReturnValue;
        }
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

    private static object? CreateCompletedResult(Type returnType, object? value)
    {
        if (returnType == typeof(Task))
        {
            return Task.CompletedTask;
        }

        if (returnType == typeof(ValueTask))
        {
            return default(ValueTask);
        }

        Type resultType = returnType.GetGenericArguments()[0];

        if (value is null && resultType.IsValueType)
        {
            value = Activator.CreateInstance(resultType);
        }

        if (returnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            return typeof(Task).GetMethod(nameof(Task.FromResult))!.MakeGenericMethod(resultType).Invoke(null, new[] { value });
        }

        return Activator.CreateInstance(returnType, value);
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

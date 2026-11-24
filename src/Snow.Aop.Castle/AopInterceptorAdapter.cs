using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using Castle.DynamicProxy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Snow.Aop.Castle;

/// <summary>
/// 把 <see cref="SnowInterceptorAttribute"/> 链适配成 Castle 拦截器: 按「类级特性 + 实现方法上的特性」组装责任链,
/// 并负责同步返回值、<see cref="Task"/>、<see cref="Task{TResult}"/>、<see cref="ValueTask"/>、<see cref="ValueTask{TResult}"/>
/// 五种返回形态的收尾。
/// <para>异步方法(返回 Task / Task&lt;T&gt; / ValueTask / ValueTask&lt;T&gt;)只把拦截链包成对应的异步结果交回, 不在拦截点阻塞调用线程;
/// 同步方法因为 Castle 的拦截点是同步的, 仍需当场拿结果。</para>
/// </summary>
internal sealed class AopInterceptorAdapter : IInterceptor
{
    private readonly Type _implementationType;
    private readonly SnowInterceptorAttribute[] _classInterceptors;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger? _logger;

    private static readonly MethodInfo AwaitChainMethod =
        typeof(AopInterceptorAdapter).GetMethod(nameof(AwaitChainAsync), BindingFlags.NonPublic | BindingFlags.Static)!;

    /// <summary>
    /// 按「实现类型 + 方法」缓存的拦截器清单。缓存是静态的: 瞬态服务每次解析都会新建适配器, 放实例字段等于每次重读一遍反射
    /// </summary>
    private static readonly ConcurrentDictionary<(Type ImplementationType, MethodInfo Method), SnowInterceptorAttribute[]> InterceptorsByMethod = new();

    /// <summary>
    /// 按「实现类型 + 方法」缓存的组装好的链, 理由同上
    /// </summary>
    private static readonly ConcurrentDictionary<(Type ImplementationType, MethodInfo Method), AspectChain> Chains = new();

    /// <summary>
    /// 只提示一次的告警去重表(键: 实现类型 + 方法(或成员名) + 告警类别)
    /// </summary>
    private static readonly ConcurrentDictionary<string, byte> WarnedMessages = new();

    /// <summary>
    /// 组装好的整条拦截链: 下一环由参数传入(与 ASP.NET Core 中间件同形), 因此可以按方法缓存, 每次调用只新建上下文
    /// </summary>
    /// <param name="context">调用上下文</param>
    /// <param name="terminal">链尾, 真正执行目标方法</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>表示整条链的任务</returns>
    private delegate Task AspectChain(AspectContext context, AspectDelegate terminal, CancellationToken cancellationToken);

    /// <summary>
    /// 创建适配器
    /// </summary>
    /// <param name="implementationType">实现类型, 用于按方法名与参数类型找实现方法上的特性</param>
    /// <param name="classInterceptors">类级拦截器</param>
    /// <param name="serviceProvider">服务提供者</param>
    public AopInterceptorAdapter(Type implementationType, SnowInterceptorAttribute[] classInterceptors, IServiceProvider serviceProvider)
    {
        _implementationType = implementationType;
        _classInterceptors = classInterceptors;
        _serviceProvider = serviceProvider;
        _logger = serviceProvider.GetService<ILogger<AopInterceptorAdapter>>();

        if (classInterceptors.Length > 0)
        {
            WarnNonVirtualMembers();
        }
    }

    /// <inheritdoc />
    /// <param name="invocation">Castle 调用信息</param>
    public void Intercept(IInvocation invocation)
    {
        MethodInfo method = invocation.MethodInvocationTarget ?? invocation.Method;

        AspectContext context = new DefaultAspectContext(invocation, _serviceProvider);

        AspectChain chain = Chains.GetOrAdd((_implementationType, method), key =>
        {
            WarnAsyncVoid(key.Method);

            return BuildChain(ResolveInterceptors(key.Method));
        });
        CancellationToken cancellationToken = ResolveCancellationToken(invocation);

        Task running = chain(context, (ctx, _) => ExecuteTerminalAsync(ctx, invocation), cancellationToken);
        Type returnType = method.ReturnType;

        if (!IsAsyncReturnType(returnType))
        {
            // 同步方法的拦截点本身是同步的, 只能在当场阻塞拿结果
            running.GetAwaiter().GetResult();
            invocation.ReturnValue = context.ReturnValue;

            return;
        }

        // 异步方法: 不阻塞调用线程, 直接把拦截链包成与声明返回类型一致的异步结果交回去,
        // 调用方 await 时才会推进目标方法(这才是真正的异步拦截; 旧实现会在此 GetAwaiter().GetResult() 卡线程)
        invocation.ReturnValue = CreateAsyncResult(returnType, running, context);
    }

    private SnowInterceptorAttribute[] ResolveInterceptors(MethodInfo method)
    {
        return InterceptorsByMethod.GetOrAdd((_implementationType, method), static (key, state) =>
        {
            List<SnowInterceptorAttribute> interceptors = new(state.ClassInterceptors);

            // 泛型方法按定义查: 构造出来的方法在实现类型里找不到同名同参的成员
            MethodInfo lookup = key.Method.IsGenericMethod ? key.Method.GetGenericMethodDefinition() : key.Method;
            MethodInfo? implementationMethod = FindImplementationMethod(state.ImplementationType, lookup);

            // 注解标在接口方法上同样生效(与标在实现方法上等效), 声明处更外层, 所以排在实现方法之前。
            // 显式实现时被拦截的方法本身就是实现方法(名字是"接口全名.方法名"), 声明处的特性只能顺着接口映射去找,
            // 否则接口方法上的注解会被静默丢掉
            MethodInfo? contractMethod = FindContractMethod(state.ImplementationType, lookup);

            // 三处各读一次: 被拦方法自己可能就带着注解(显式实现时它就是实现方法), 不能因为"找到的实现方法等于它"就跳过
            if (contractMethod is not null)
            {
                interceptors.AddRange(contractMethod.GetCustomAttributes<SnowInterceptorAttribute>(true));
            }

            if (implementationMethod is not null && implementationMethod != contractMethod)
            {
                interceptors.AddRange(implementationMethod.GetCustomAttributes<SnowInterceptorAttribute>(true));
            }

            if (lookup != contractMethod && lookup != implementationMethod)
            {
                interceptors.AddRange(lookup.GetCustomAttributes<SnowInterceptorAttribute>(true));
            }

            return interceptors.ToArray();
        }, (ImplementationType: _implementationType, ClassInterceptors: _classInterceptors));
    }

    /// <summary>
    /// 找到被拦截方法在接口上的声明: 方法本身就在接口上时直接返回, 是类上的实现(含显式实现)时顺接口映射反查
    /// </summary>
    /// <param name="implementationType">实现类型</param>
    /// <param name="method">被拦截的方法</param>
    /// <returns>接口上的声明方法; 找不到时返回 <see langword="null"/></returns>
    private static MethodInfo? FindContractMethod(Type implementationType, MethodInfo method)
    {
        if (method.DeclaringType is { IsInterface: true })
        {
            return method;
        }

        foreach (Type contract in implementationType.GetInterfaces())
        {
            InterfaceMapping map;

            try
            {
                map = implementationType.GetInterfaceMap(contract);
            }
            catch (ArgumentException)
            {
                // 开放泛型等取不到映射的情形, 跳过这个接口
                continue;
            }

            int index = Array.IndexOf(map.TargetMethods, method);
            if (index >= 0)
            {
                return map.InterfaceMethods[index];
            }
        }

        return null;
    }

    /// <summary>
    /// 找到接口方法(或类方法)对应的实现方法。
    /// <para>不能只按方法名找: 显式接口实现的方法名是"接口全名.方法名"且是 private, 按名字一个都找不到,
    /// 接口方法上的注解与显式实现上的注解都会因此被静默丢掉, 所以先走接口映射。</para>
    /// </summary>
    /// <param name="implementationType">实现类型</param>
    /// <param name="method">被拦截的方法(接口方法或实现方法)</param>
    /// <returns>实现方法; 找不到时返回 <see langword="null"/></returns>
    private static MethodInfo? FindImplementationMethod(Type implementationType, MethodInfo method)
    {
        // 被拦方法本身就在实现类型上(普通方法或显式实现的方法), 不需要再找
        if (method.DeclaringType == implementationType)
        {
            return method;
        }

        if (method.DeclaringType is { IsInterface: true } contract && contract.IsAssignableFrom(implementationType))
        {
            InterfaceMapping map = implementationType.GetInterfaceMap(contract);
            int index = Array.IndexOf(map.InterfaceMethods, method);

            if (index >= 0)
            {
                return map.TargetMethods[index];
            }
        }

        return implementationType.GetMethod(
            method.Name,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            binder: null,
            types: method.GetParameters().Select(parameter => parameter.ParameterType).ToArray(),
            modifiers: null);
    }

    /// <summary>
    /// async void 的拦截点看不到完成: 拦截链只能等到方法真正返回第一个 await 之前, 后置逻辑会提前执行
    /// </summary>
    /// <param name="method">被拦截的方法</param>
    private void WarnAsyncVoid(MethodInfo method)
    {
        if (method.ReturnType != typeof(void) || !method.IsDefined(typeof(AsyncStateMachineAttribute), false))
        {
            return;
        }

        WarnOnce(
            "asyncvoid:" + _implementationType.FullName + "." + method.Name,
            "AOP 拦不到 " + _implementationType.FullName + "." + method.Name + " 的完成: 它是 async void, 拦截链在方法真正结束之前就收尾了(后置逻辑提前执行、看不到返回值)。改成 async Task。");
    }

    /// <summary>
    /// 类级拦截器覆盖全类, 但 Castle 的类代理只能拦 virtual: 非 virtual 成员会被静默跳过, 这里一次性点出来
    /// </summary>
    private void WarnNonVirtualMembers()
    {
        List<string> skipped = new();

        foreach (MethodInfo method in _implementationType.GetMethods(BindingFlags.Public | BindingFlags.Instance))
        {
            if (method.DeclaringType == typeof(object) || method.IsVirtual && !method.IsFinal)
            {
                continue;
            }

            skipped.Add(method.Name);
        }

        if (skipped.Count == 0)
        {
            return;
        }

        WarnOnce(
            "nonvirtual:" + _implementationType.FullName,
            "AOP 类代理只拦 virtual 方法, " + _implementationType.FullName + " 上这些成员有类级拦截器但不会被拦: "
                + string.Join(", ", skipped.Take(5))
                + (skipped.Count > 5 ? " 等 " + skipped.Count + " 个" : string.Empty)
                + "。要拦就把它们改成 virtual。");
    }

    /// <summary>
    /// 同一类告警只打一次: 适配器按代理实例创建, 不去重的话瞬态服务会刷屏
    /// </summary>
    /// <param name="key">去重键</param>
    /// <param name="message">告警内容</param>
    private void WarnOnce(string key, string message)
    {
        if (!WarnedMessages.TryAdd(key, 0))
        {
            return;
        }

        _logger?.LogWarning(message);
    }

    /// <summary>
    /// 把拦截器数组组装成一条链: 末尾接调用方传入的 terminal, 链本身可以按方法缓存
    /// </summary>
    /// <param name="interceptors">按执行顺序排好的拦截器</param>
    /// <returns>组装好的链</returns>
    private static AspectChain BuildChain(SnowInterceptorAttribute[] interceptors)
    {
        AspectChain next = static (context, terminal, cancellationToken) => terminal(context, cancellationToken);

        for (int index = interceptors.Length - 1; index >= 0; index--)
        {
            SnowInterceptorAttribute interceptor = interceptors[index];
            AspectChain inner = next;

            next = (context, terminal, cancellationToken) =>
                interceptor.InvokeAsync(context, (ctx, ct) => inner(ctx, terminal, ct), cancellationToken);
        }

        return next;
    }

    /// <summary>
    /// 取被拦截方法签名里的第一个 CancellationToken, 没有就是 None
    /// </summary>
    /// <param name="context">Castle 的调用信息</param>
    /// <returns>取消令牌</returns>
    private static CancellationToken ResolveCancellationToken(IInvocation context)
    {
        foreach (object? argument in context.Arguments)
        {
            if (argument is CancellationToken token)
            {
                return token;
            }
        }

        return CancellationToken.None;
    }

    /// <summary>
    /// 链尾: 真正执行目标方法, 结果写回上下文
    /// </summary>
    /// <param name="context">调用上下文</param>
    /// <param name="invocation">Castle 的调用信息</param>
    /// <returns>表示执行过程的任务</returns>
    private static async Task ExecuteTerminalAsync(AspectContext context, IInvocation invocation)
    {
        context.ReturnValue = await ExecuteTargetAsync(invocation).ConfigureAwait(false);
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
    /// <param name="context">调用上下文</param>
    /// <returns>与声明返回类型一致的异步结果</returns>
    private static object CreateAsyncResult(Type returnType, Task chain, AspectContext context)
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
        object taskOfResult = AwaitChainMethod.MakeGenericMethod(resultType).Invoke(null, [chain, context])!;

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
    /// <param name="context">调用上下文</param>
    /// <returns>拦截结果</returns>
    private static async Task<TResult> AwaitChainAsync<TResult>(Task chain, AspectContext context)
    {
        await chain.ConfigureAwait(false);

        if (context.ReturnValue is TResult result)
        {
            return result;
        }

        if (context.ReturnValue is null)
        {
            // 短路且未给返回值时按声明类型给默认值(值类型不能返回 null)
            return default!;
        }

        throw new InvalidCastException(
            "拦截器给出的返回值类型与声明返回类型不符: 声明 "
                + typeof(TResult).FullName
                + ", 实际 "
                + context.ReturnValue.GetType().FullName
                + "。"
        );
    }

    private static async Task<object?> ExecuteTargetAsync(IInvocation context)
    {
        context.Proceed();

        object? returned = context.ReturnValue;

        switch (returned)
        {
            case null:
                return null;

            case Task task:
            {
                await task.ConfigureAwait(false);

                // 用声明类型判断: async Task 的运行时类型其实是泛型的 Task<VoidTaskResult>
                return context.Method.ReturnType.IsGenericType
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

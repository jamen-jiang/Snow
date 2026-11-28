using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using Snow.DynamicProxy;
using Snow.Reflection;

namespace Snow.Castle.DynamicProxy;

/// <summary>
/// 返回 <see cref="IAsyncEnumerable{T}"/> 的方法无法走普通的异步拦截路径(Task 语义), 这里单独处理:
/// 把返回值换成「拦截链启动后按需枚举」的包装对象, 与 ABP 的 <c>CastleAsyncEnumerableAbpInterceptor</c> 同形。
/// </summary>
internal static class CastleAsyncSnowInterceptor
{
    private delegate object AsyncEnumerableHandler(IInvocation invocation, ISnowInterceptor interceptor);

    private static readonly MethodInfo InterceptMethod = typeof(CastleAsyncSnowInterceptor)
        .GetMethod(nameof(Intercept), BindingFlags.Static | BindingFlags.NonPublic)!;

    private static readonly ConcurrentDictionary<Type, AsyncEnumerableHandler> Handlers = new();

    private static readonly ConcurrentDictionary<MethodInfo, bool> MethodsWithByRefParameters = new();

    /// <summary>
    /// 尝试按异步流拦截: 返回类型不是 <see cref="IAsyncEnumerable{T}"/> 时返回 <see langword="false"/>
    /// </summary>
    /// <param name="invocation">Castle 的调用信息</param>
    /// <param name="interceptor">实际的拦截器</param>
    /// <returns>已替换返回值时返回 <see langword="true"/></returns>
    public static bool TryIntercept(IInvocation invocation, ISnowInterceptor interceptor)
    {
        Type returnType = invocation.Method.ReturnType;
        if (!TypeHelper.IsAsyncEnumerable(returnType))
        {
            return false;
        }

        if (MethodsWithByRefParameters.GetOrAdd(invocation.Method, method => method.GetParameters().Any(parameter => parameter.ParameterType.IsByRef)))
        {
            return false;
        }

        invocation.ReturnValue = Handlers.GetOrAdd(returnType, CreateHandler)(invocation, interceptor);
        return true;
    }

    /// <summary>
    /// 为具体的异步流元素类型造一个处理器委托
    /// </summary>
    /// <param name="returnType">返回类型(某个 <see cref="IAsyncEnumerable{T}"/> 构造)</param>
    /// <returns>能把调用换成包装对象的处理器</returns>
    private static AsyncEnumerableHandler CreateHandler(Type returnType)
    {
        return (AsyncEnumerableHandler)InterceptMethod
            .MakeGenericMethod(returnType.GetGenericArguments()[0])
            .CreateDelegate(typeof(AsyncEnumerableHandler));
    }

    /// <summary>
    /// 按元素类型 <typeparamref name="T"/> 建立拦截链启动任务与调用适配器, 返回可重复枚举的包装对象
    /// </summary>
    /// <typeparam name="T">异步流的元素类型</typeparam>
    /// <param name="invocation">Castle 的调用信息</param>
    /// <param name="interceptor">实际的拦截器</param>
    /// <returns>包装后的异步流</returns>
    private static object Intercept<T>(IInvocation invocation, ISnowInterceptor interceptor)
    {
        CastleSnowMethodInvocationAdapterWithReturnValue<IAsyncEnumerable<T>> methodInvocation =
            new(invocation,
                invocation.CaptureProceedInfo(),
                ProceedAsync<T>);

        return new InterceptedAsyncEnumerable<T>(InterceptAsync(interceptor, methodInvocation), methodInvocation);
    }

    /// <summary>
    /// 启动拦截链; 拦截器同步抛出的异常也在这里捕获, 留到枚举时再抛
    /// </summary>
    /// <param name="interceptor">实际的拦截器</param>
    /// <param name="methodInvocation">方法调用</param>
    /// <returns>表示拦截过程的任务</returns>
    private static async Task InterceptAsync(ISnowInterceptor interceptor, ISnowMethodInvocation methodInvocation)
    {
        // 拦截器同步抛出的异常也一起捕获, 让它像 Task 一样在枚举时才抛出
        await interceptor.InterceptAsync(methodInvocation);
    }

    /// <summary>
    /// 真正执行目标方法, 并把下一环包装的异步流还原成可枚举的源
    /// </summary>
    /// <typeparam name="T">异步流的元素类型</typeparam>
    /// <param name="invocation">Castle 的调用信息</param>
    /// <param name="proceedInfo">继续执行下一环所需的信息</param>
    /// <returns>目标方法返回的异步流</returns>
    private static async Task<IAsyncEnumerable<T>> ProceedAsync<T>(IInvocation invocation, IInvocationProceedInfo proceedInfo)
    {
        proceedInfo.Invoke();

        // 等待下一环拦截器, 相当于 await 一个返回 Task 的方法
        if (invocation.ReturnValue is InterceptedAsyncEnumerable<T> next)
        {
            return (await next.GetResultAsync())!;
        }

        return (IAsyncEnumerable<T>)invocation.ReturnValue!;
    }

    private sealed class InterceptedAsyncEnumerable<T> : IAsyncEnumerable<T>
    {
        private readonly Task _interceptTask;
        private readonly ISnowMethodInvocation _methodInvocation;

        public InterceptedAsyncEnumerable(Task interceptTask, ISnowMethodInvocation methodInvocation)
        {
            _interceptTask = interceptTask;
            _methodInvocation = methodInvocation;
        }

        /// <summary>
        /// 等拦截链跑完, 取回最终的异步流
        /// </summary>
        /// <returns>拦截链产出的异步流; 目标方法返回 <c>null</c> 时为 <c>null</c></returns>
        public async Task<IAsyncEnumerable<T>?> GetResultAsync()
        {
            await _interceptTask;
            return (IAsyncEnumerable<T>?)_methodInvocation.ReturnValue;
        }

        /// <inheritdoc />
        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return EnumerateAsync(cancellationToken).GetAsyncEnumerator(cancellationToken);
        }

        /// <summary>
        /// 先等拦截链跑完拿到真正的源, 再逐个元素转出去
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>目标方法返回的异步流</returns>
        private async IAsyncEnumerable<T> EnumerateAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            // 即使调用方抑制了执行流, 也要为下面的 await 保住调用方的执行上下文; 这一步结束时调用方自己的上下文会恢复
            if (ExecutionContext.IsFlowSuppressed())
            {
                ExecutionContext.RestoreFlow();
            }

            IAsyncEnumerable<T>? source = await GetResultAsync();
            if (source == null)
            {
                yield break;
            }

            await foreach (T item in source.WithCancellation(cancellationToken))
            {
                yield return item;
            }
        }
    }
}

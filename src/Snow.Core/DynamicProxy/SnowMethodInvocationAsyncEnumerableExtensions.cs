using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Snow.Reflection;

namespace Snow.DynamicProxy;

/// <summary>
/// 让返回 <see cref="IAsyncEnumerable{T}"/> 的方法也能被作用域覆盖: 把返回的异步流换成「每次枚举都先开一个新作用域」的包装。
/// </summary>
internal static class SnowMethodInvocationAsyncEnumerableExtensions
{
    private delegate object WrapHandler(object source, Func<ISnowInterceptionScope> beginScope);

    private static readonly MethodInfo WrapMethod = typeof(SnowMethodInvocationAsyncEnumerableExtensions)
        .GetMethod(nameof(Wrap), BindingFlags.Static | BindingFlags.NonPublic)!;

    private static readonly ConcurrentDictionary<MethodInfo, WrapHandler?> WrapHandlers = new();

    /// <summary>
    /// 在返回的异步流外层套上作用域: 每次枚举都新建一个作用域。
    /// <paramref name="beginScope"/> 必须同步设置异步本地值(例如环境工作单元)。
    /// </summary>
    /// <param name="invocation">调用信息</param>
    /// <param name="beginScope">作用域工厂</param>
    /// <returns>替换了返回值时返回 <see langword="true"/></returns>
    public static bool WrapAsyncEnumerable(this ISnowMethodInvocation invocation, Func<ISnowInterceptionScope> beginScope)
    {
        WrapHandler? handler = WrapHandlers.GetOrAdd(invocation.Method, CreateHandler);
        if (handler == null || invocation.ReturnValue == null)
        {
            return false;
        }

        invocation.ReturnValue = handler(invocation.ReturnValue, beginScope);
        return true;
    }

    /// <summary>
    /// 按方法返回的异步流元素类型造一个包装委托; 不能包装(非异步流 / 带 ref、out 参数)时返回 <c>null</c>
    /// </summary>
    /// <param name="method">被拦截的方法</param>
    /// <returns>包装委托; 不适用时为 <c>null</c></returns>
    private static WrapHandler? CreateHandler(MethodInfo method)
    {
        // 带 ref/out 参数的方法返回值不能被替换(它也没法按异步方式拦截), 直接放弃包装
        Type returnType = method.ReturnType;
        if (!TypeHelper.IsAsyncEnumerable(returnType) ||
            method.GetParameters().Any(parameter => parameter.ParameterType.IsByRef))
        {
            return null;
        }

        return (WrapHandler)WrapMethod
            .MakeGenericMethod(returnType.GenericTypeArguments[0])
            .CreateDelegate(typeof(WrapHandler));
    }

    /// <summary>
    /// 把源异步流换成「每次枚举先开作用域」的包装流
    /// </summary>
    /// <typeparam name="T">异步流的元素类型</typeparam>
    /// <param name="source">原异步流</param>
    /// <param name="beginScope">作用域工厂</param>
    /// <returns>包装后的异步流</returns>
    private static object Wrap<T>(object source, Func<ISnowInterceptionScope> beginScope)
    {
        return EnumerateInScopeAsync((IAsyncEnumerable<T>)source, beginScope);
    }

    /// <summary>
    /// 枚举源异步流: 每一步都在捕获的执行上下文里执行, 结束时按是否出错调用失败或完成收尾, 最后释放作用域
    /// </summary>
    /// <typeparam name="T">异步流的元素类型</typeparam>
    /// <param name="source">原异步流</param>
    /// <param name="beginScope">作用域工厂</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>包装后的异步流</returns>
    private static async IAsyncEnumerable<T> EnumerateInScopeAsync<T>(
        IAsyncEnumerable<T> source,
        Func<ISnowInterceptionScope> beginScope,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ISnowInterceptionScope scope = beginScope();

        // 异步迭代器的每一步都在调用方的执行上下文里跑, 拿不到 beginScope 设置的异步本地值, 所以要自己捕获并还原
        ExecutionContext context = CaptureExecutionContext();

        try
        {
            IAsyncEnumerator<T> enumerator;
            try
            {
                enumerator = Run(context, () => source.GetAsyncEnumerator(cancellationToken));
            }
            catch (Exception exception)
            {
                await Run(context, () => scope.FailAsync(exception));
                throw;
            }

            Exception? sourceException = null;

            try
            {
                while (true)
                {
                    bool hasNext;
                    T current = default!;
                    try
                    {
                        hasNext = await Run(context, () => MoveNextAsync(enumerator));
                        if (hasNext)
                        {
                            current = Run(context, () => enumerator.Current);
                        }
                    }
                    catch (Exception exception)
                    {
                        sourceException = exception;
                        throw;
                    }

                    if (!hasNext)
                    {
                        break;
                    }

                    yield return current;
                }
            }
            finally
            {
                // 先释放数据源, 让作用域也覆盖它的清理过程
                try
                {
                    await Run(context, () => DisposeAsync(enumerator));
                }
                catch (Exception exception)
                {
                    // 数据源自身的异常优先抛给调用方
                    sourceException = exception;
                    throw;
                }
                finally
                {
                    if (sourceException != null)
                    {
                        await Run(context, () => scope.FailAsync(sourceException));
                    }
                    else
                    {
                        // 调用方提前停止枚举时也会走到这里
                        await Run(context, () => scope.CompleteAsync(cancellationToken));
                    }
                }
            }
        }
        finally
        {
            Run(context, () =>
            {
                scope.Dispose();
                return true;
            });
        }
    }

    /// <summary>
    /// 捕获当前的执行上下文(含异步本地值), 供异步迭代器的每一步还原
    /// </summary>
    /// <returns>执行上下文</returns>
    private static ExecutionContext CaptureExecutionContext()
    {
        // 流被抑制时 Capture 返回 null; 还原不影响调用方, 这一步结束时调用方自己的执行上下文会恢复
        if (ExecutionContext.IsFlowSuppressed())
        {
            ExecutionContext.RestoreFlow();
        }

        return ExecutionContext.Capture()!;
    }

    // 数据源的 ValueTask(例如 IValueTaskSource)在完成时与读结果时都可能继续跑代码, 因此单独包一层
    /// <summary>
    /// 包一层 <see cref="IAsyncEnumerator{T}.MoveNextAsync"/>, 让上下文还原能覆盖它的异步续体
    /// </summary>
    /// <typeparam name="T">异步流的元素类型</typeparam>
    /// <param name="enumerator">枚举器</param>
    /// <returns>是否还有下一个元素</returns>
    private static async ValueTask<bool> MoveNextAsync<T>(IAsyncEnumerator<T> enumerator)
    {
        return await enumerator.MoveNextAsync();
    }

    /// <summary>
    /// 包一层 <see cref="IAsyncDisposable.DisposeAsync"/>, 让上下文还原能覆盖它的异步续体
    /// </summary>
    /// <typeparam name="T">异步流的元素类型</typeparam>
    /// <param name="enumerator">枚举器</param>
    /// <returns>表示释放过程的任务</returns>
    private static async ValueTask DisposeAsync<T>(IAsyncEnumerator<T> enumerator)
    {
        await enumerator.DisposeAsync();
    }

    /// <summary>
    /// 在指定执行上下文的副本里同步执行一段代码
    /// </summary>
    /// <typeparam name="TResult">返回值类型</typeparam>
    /// <param name="context">执行上下文</param>
    /// <param name="func">要执行的代码</param>
    /// <returns>代码的返回值</returns>
    private static TResult Run<TResult>(ExecutionContext context, Func<TResult> func)
    {
        TResult result = default!;
        ExecutionContext.Run(context.CreateCopy(), _ => result = func(), null);
        return result;
    }
}

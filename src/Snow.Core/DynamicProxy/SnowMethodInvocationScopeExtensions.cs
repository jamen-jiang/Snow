using System;
using System.Threading.Tasks;

namespace Snow.DynamicProxy;

/// <summary>
/// <see cref="ISnowMethodInvocation"/> 的作用域扩展方法。
/// </summary>
internal static class SnowMethodInvocationScopeExtensions
{
    /// <summary>
    /// 在作用域内执行调用: 正常结束调 CompleteAsync, 抛异常调 FailAsync 后原样抛出, 最后释放作用域。
    /// <para>返回 <see cref="System.Collections.Generic.IAsyncEnumerable{T}"/> 时, 在其每次枚举时也会新建一个作用域。</para>
    /// </summary>
    /// <param name="invocation">调用信息</param>
    /// <param name="beginScope">作用域工厂; 必须同步设置异步本地值</param>
    /// <returns>表示执行过程的任务</returns>
    public static async Task ProceedInScopeAsync(this ISnowMethodInvocation invocation, Func<ISnowInterceptionScope> beginScope)
    {
        using (ISnowInterceptionScope scope = beginScope())
        {
            try
            {
                await invocation.ProceedAsync();
            }
            catch (Exception exception)
            {
                await scope.FailAsync(exception);
                throw;
            }

            await scope.CompleteAsync(default);
        }

        invocation.WrapAsyncEnumerable(beginScope);
    }
}

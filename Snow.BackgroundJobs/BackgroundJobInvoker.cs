using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Snow.BackgroundJobs;

/// <summary>
/// 执行一条已排队的任务: 任务名 → 参数类型/作业类型 → 反序列化 → 执行
/// <para>内置 worker 与 Hangfire 回放都走这里, 执行语义只有一份。</para>
/// </summary>
public static class BackgroundJobInvoker
{
    /// <summary>
    /// 按任务名执行一条任务
    /// </summary>
    /// <param name="serviceProvider">作业实例从这里解析(通常是当前作用域)</param>
    /// <param name="options">任务配置</param>
    /// <param name="jobName">任务名</param>
    /// <param name="jobArgs">序列化后的任务参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>表示执行过程的任务</returns>
    public static Task RunAsync(IServiceProvider serviceProvider, BackgroundJobOptions options, string jobName, string jobArgs, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(jobName);
        ArgumentNullException.ThrowIfNull(jobArgs);

        Type jobType = options.GetJobType(jobName);
        Type argsType = options.GetArgsType(jobName);
        object? args = BackgroundJobSerializer.Deserialize(jobArgs, argsType);

        return ExecuteAsync(serviceProvider, jobType, args, cancellationToken);
    }

    /// <summary>
    /// 执行一个已还原参数的作业
    /// </summary>
    /// <param name="serviceProvider">作业实例从这里解析</param>
    /// <param name="jobType">作业实现类型</param>
    /// <param name="args">任务参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>表示执行过程的任务</returns>
    public static async Task ExecuteAsync(IServiceProvider serviceProvider, Type jobType, object? args, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(jobType);

        object instance = ActivatorUtilities.GetServiceOrCreateInstance(serviceProvider, jobType);

        MethodInfo? executeMethod =
            jobType.GetMethod(nameof(IBackgroundJob<object>.Execute), BindingFlags.Public | BindingFlags.Instance) ??
            jobType.GetMethod(nameof(IAsyncBackgroundJob<object>.ExecuteAsync), BindingFlags.Public | BindingFlags.Instance) ??
            throw new InvalidOperationException($"作业类型 {jobType.FullName} 既没有 Execute 也没有 ExecuteAsync 方法。");

        object?[] arguments = executeMethod.GetParameters().Length == 1 ? [args] : [args, cancellationToken];

        // TargetInvocationException 只包一层执行期异常, 直接把内层异常抛给调用方, 别让日志里多一层噪音
        try
        {
            object? result = executeMethod.Invoke(instance, arguments);

            switch (result)
            {
                case Task task:
                    await task.ConfigureAwait(false);
                    break;
                case ValueTask valueTask:
                    await valueTask.ConfigureAwait(false);
                    break;
            }
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            throw exception.InnerException;
        }
    }
}

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Snow.BackgroundJobs;

/// <summary>
/// 默认任务执行器: 从作用域解析作业类(未登记时用 ActivatorUtilities 现建), 反射调用 Execute 或 ExecuteAsync
/// </summary>
public class BackgroundJobExecuter : IBackgroundJobExecuter
{
    /// <inheritdoc />
    public virtual async Task ExecuteAsync(JobExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        object instance = ActivatorUtilities.GetServiceOrCreateInstance(context.ServiceProvider, context.JobType);

        MethodInfo? executeMethod =
            context.JobType.GetMethod(nameof(IBackgroundJob<object>.Execute), BindingFlags.Public | BindingFlags.Instance) ??
            context.JobType.GetMethod(nameof(IAsyncBackgroundJob<object>.ExecuteAsync), BindingFlags.Public | BindingFlags.Instance);

        if (executeMethod is null)
        {
            throw new InvalidOperationException($"作业类型 {context.JobType.FullName} 既没有 Execute 也没有 ExecuteAsync 方法。");
        }

        object?[] arguments = executeMethod.GetParameters().Length == 1
            ? [context.JobArgs]
            : [context.JobArgs, context.CancellationToken];

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
            throw new BackgroundJobExecutionException($"后台任务 {context.JobType.FullName} 执行失败。", exception.InnerException)
            {
                JobType = context.JobType.AssemblyQualifiedName ?? context.JobType.FullName ?? string.Empty,
                JobArgs = context.JobArgs
            };
        }
        catch (Exception exception) when (exception is not BackgroundJobExecutionException)
        {
            throw new BackgroundJobExecutionException($"后台任务 {context.JobType.FullName} 执行失败。", exception)
            {
                JobType = context.JobType.AssemblyQualifiedName ?? context.JobType.FullName ?? string.Empty,
                JobArgs = context.JobArgs
            };
        }
    }
}

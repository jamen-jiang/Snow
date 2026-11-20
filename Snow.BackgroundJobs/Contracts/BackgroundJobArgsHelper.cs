namespace Snow.BackgroundJobs;

/// <summary>
/// 任务参数类型解析: 从 <see cref="IBackgroundJob{TArgs}"/> 或 <see cref="IAsyncBackgroundJob{TArgs}"/> 的泛型参数取出参数类型
/// </summary>
internal static class BackgroundJobArgsHelper
{
    /// <summary>
    /// 取任务实现的参数类型
    /// </summary>
    /// <param name="jobType">任务实现类型</param>
    /// <returns>参数类型</returns>
    public static Type GetJobArgsType(Type jobType)
    {
        ArgumentNullException.ThrowIfNull(jobType);

        foreach (Type @interface in jobType.GetInterfaces())
        {
            if (!@interface.IsGenericType)
            {
                continue;
            }

            Type definition = @interface.GetGenericTypeDefinition();
            if (definition != typeof(IBackgroundJob<>) && definition != typeof(IAsyncBackgroundJob<>))
            {
                continue;
            }

            Type[] genericArgs = @interface.GetGenericArguments();
            if (genericArgs.Length == 1)
            {
                return genericArgs[0];
            }
        }

        throw new InvalidOperationException($"类型 {jobType.FullName} 未实现 {typeof(IBackgroundJob<>).Name} 或 {typeof(IAsyncBackgroundJob<>).Name}, 无法确定任务参数类型。");
    }
}

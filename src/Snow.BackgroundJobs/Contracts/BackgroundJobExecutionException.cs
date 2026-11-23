using System;

namespace Snow.BackgroundJobs;

/// <summary>
/// 任务执行失败: 抛出这个异常表示"这次失败可以重试"
/// <para>任务内部的业务异常由调用器统一包装成这个类型; 取不到任务类型、反射找不到执行方法、参数反序列化失败等不可重试的错误不走这里。</para>
/// </summary>
public class BackgroundJobExecutionException : Exception
{
    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="message">异常说明</param>
    /// <param name="innerException">任务内部抛出的原始异常</param>
    /// <param name="jobType">任务实现类型</param>
    /// <param name="jobArgs">任务参数</param>
    public BackgroundJobExecutionException(string message, Exception innerException, Type? jobType = null, object? jobArgs = null)
        : base(message, innerException)
    {
        JobType = jobType;
        JobArgs = jobArgs;
    }

    /// <summary>
    /// 任务实现类型
    /// </summary>
    public Type? JobType { get; }

    /// <summary>
    /// 任务参数
    /// </summary>
    public object? JobArgs { get; }
}

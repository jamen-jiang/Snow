namespace Snow.BackgroundJobs;

/// <summary>
/// 排队中的任务: 只有执行所需的最小信息
/// </summary>
public class BackgroundJobInfo
{
    /// <summary>
    /// 任务 Id
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 任务名(默认取参数类型全名), 执行时用它找回参数类型与作业类型
    /// </summary>
    public string JobName { get; set; } = string.Empty;

    /// <summary>
    /// 序列化后的任务参数
    /// </summary>
    public string JobArgs { get; set; } = string.Empty;

    /// <summary>
    /// 创建时间(UTC)
    /// </summary>
    public DateTime CreationTime { get; set; }

    /// <summary>
    /// 下次可执行时间(UTC)
    /// </summary>
    public DateTime NextTryTime { get; set; }

    /// <summary>
    /// 已尝试次数
    /// </summary>
    public int TryCount { get; set; }

    /// <summary>
    /// 是否已放弃(超过重试上限)
    /// </summary>
    public bool IsAbandoned { get; set; }

    /// <summary>
    /// 浅拷贝: 存储层的进出都用副本, 调用方原地修改不会污染已落库的数据
    /// </summary>
    /// <returns>副本</returns>
    public BackgroundJobInfo Clone()
    {
        return (BackgroundJobInfo)MemberwiseClone();
    }
}

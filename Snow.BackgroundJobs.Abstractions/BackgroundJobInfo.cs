namespace Snow.BackgroundJobs;

/// <summary>
/// 后台任务记录: 入队时落库,  worker 轮询取出的就是这条记录
/// </summary>
public class BackgroundJobInfo
{
    /// <summary>
    /// 任务 Id
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 任务名(默认取参数类型全名)
    /// </summary>
    public string JobName { get; set; } = default!;

    /// <summary>
    /// 序列化后的任务参数
    /// </summary>
    public string JobArgs { get; set; } = default!;

    /// <summary>
    /// 已尝试次数
    /// </summary>
    public short TryCount { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreationTime { get; set; }

    /// <summary>
    /// 下次可执行时间(延迟任务与失败退避都靠它)
    /// </summary>
    public DateTime NextTryTime { get; set; }

    /// <summary>
    /// 上次执行时间
    /// </summary>
    public DateTime? LastTryTime { get; set; }

    /// <summary>
    /// 是否已放弃(连续失败超过上限时置真)
    /// </summary>
    public bool IsAbandoned { get; set; }

    /// <summary>
    /// 优先级
    /// </summary>
    public BackgroundJobPriority Priority { get; set; } = BackgroundJobPriority.Normal;
}

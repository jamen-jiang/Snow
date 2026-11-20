namespace Snow.BackgroundJobs;

/// <summary>
/// 后台任务优先级: 数值越大越先执行(与 ABP 取值一致)
/// </summary>
public enum BackgroundJobPriority : byte
{
    /// <summary>
    /// 低
    /// </summary>
    Low = 5,

    /// <summary>
    /// 偏低
    /// </summary>
    BelowNormal = 10,

    /// <summary>
    /// 普通(默认)
    /// </summary>
    Normal = 15,

    /// <summary>
    /// 偏高
    /// </summary>
    AboveNormal = 20,

    /// <summary>
    /// 高
    /// </summary>
    High = 25
}

namespace Snow.Workflows;

/// <summary>
/// 一次工作流执行的结果快照。状态用字符串透出, 避免把某一引擎的枚举泄漏到业务层。
/// </summary>
public sealed record SnowWorkflowResult
{
    /// <summary>
    /// 工作流实例标识(不是定义标识)。
    /// </summary>
    public required string WorkflowInstanceId { get; init; }

    /// <summary>
    /// 引擎状态名, 例如 Running / Finished。
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// 引擎子状态名, 例如 Executing / Finished / Faulted / Suspended / Cancelled。
    /// </summary>
    public required string SubStatus { get; init; }

    /// <summary>
    /// 是否正常跑完(状态 Finished 且子状态 Finished)。
    /// </summary>
    public bool Succeeded => Status == "Finished" && SubStatus == "Finished";

    /// <summary>
    /// 执行中记录的事故(引擎的 incident), 为空表示没有。
    /// </summary>
    public IReadOnlyCollection<string> Incidents { get; init; } = Array.Empty<string>();

    /// <summary>
    /// 工作流输出。
    /// </summary>
    public IReadOnlyDictionary<string, object> Output { get; init; } = new Dictionary<string, object>();
}

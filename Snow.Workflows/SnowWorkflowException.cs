namespace Snow.Workflows;

/// <summary>
/// 工作流执行失败: 定义找不到、执行抛错、或引擎未安装。
/// </summary>
public class SnowWorkflowException : Exception
{
    /// <summary>
    /// 创建异常。
    /// </summary>
    public SnowWorkflowException(string message) : base(message)
    {
    }

    /// <summary>
    /// 创建异常, 带上导致失败的定义标识或类型名。
    /// </summary>
    public SnowWorkflowException(string definitionId, string message) : base(message)
    {
        DefinitionId = definitionId;
    }

    /// <summary>
    /// 创建异常, 包装引擎抛出的原始异常。
    /// </summary>
    public SnowWorkflowException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// 导致失败的工作流定义标识或类型名。
    /// </summary>
    public string? DefinitionId { get; }
}

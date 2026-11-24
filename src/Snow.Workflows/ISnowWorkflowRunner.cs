namespace Snow.Workflows;

/// <summary>
/// 工作流启动器: 按定义标识或按工作流类型启动一次执行, 返回结果快照。
/// 具体引擎由实现包提供(如 Snow.Workflows.Elsa), 业务代码只依赖这个接口。
/// </summary>
public interface ISnowWorkflowRunner
{
    /// <summary>
    /// 启动一个已发布的工作流定义。
    /// </summary>
    /// <param name="definitionId">工作流定义标识(定义本身不是实例)。</param>
    /// <param name="input">可选输入, 键为工作流的输入名。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task<SnowWorkflowResult> StartAsync(string definitionId, IDictionary<string, object?>? input = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 启动一个以代码定义的工作流类型(通常是引擎的工作流基类子类)。
    /// </summary>
    /// <param name="workflowType">工作流类型。</param>
    /// <param name="input">可选输入。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task<SnowWorkflowResult> StartAsync(Type workflowType, IDictionary<string, object?>? input = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// 泛型便捷入口: 省掉 typeof(TWorkflow)。
/// </summary>
public static class SnowWorkflowRunnerExtensions
{
    /// <summary>
    /// 启动一个以代码定义的工作流类型。
    /// </summary>
    /// <param name="runner">工作流运行器</param>
    /// <param name="input">输入, 可为 null</param>
    /// <param name="cancellationToken">取消令牌</param>
    public static Task<SnowWorkflowResult> StartAsync<TWorkflow>(this ISnowWorkflowRunner runner, IDictionary<string, object?>? input = null, CancellationToken cancellationToken = default)
        where TWorkflow : class
        => runner.StartAsync(typeof(TWorkflow), input, cancellationToken);
}

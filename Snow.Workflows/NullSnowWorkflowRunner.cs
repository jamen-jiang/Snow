using Snow.DependencyInjection;

namespace Snow.Workflows;

/// <summary>
/// 静默实现: 没装引擎包时也保证 DI 能解析 ISnowWorkflowRunner, 但一调用就报错,
/// 而不是悄悄返回一个空结果让人以为跑成功了。
/// </summary>
[TransientService]
public sealed class NullSnowWorkflowRunner : ISnowWorkflowRunner
{
    private const string Message = "工作流未执行: 当前是静默实现, 请引入 Snow.Workflows.Elsa 包。";

    /// <inheritdoc />
    public Task<SnowWorkflowResult> StartAsync(string definitionId, IDictionary<string, object?>? input = null, CancellationToken cancellationToken = default)
        => throw new SnowWorkflowException(definitionId, Message);

    /// <inheritdoc />
    public Task<SnowWorkflowResult> StartAsync(Type workflowType, IDictionary<string, object?>? input = null, CancellationToken cancellationToken = default)
        => throw new SnowWorkflowException(workflowType.FullName ?? workflowType.Name, Message);
}

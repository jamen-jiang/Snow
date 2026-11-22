using Elsa.Common.Models;
using Elsa.Workflows;
using Elsa.Workflows.Management;
using Elsa.Workflows.Models;
using Elsa.Workflows.Options;
using Microsoft.Extensions.DependencyInjection;
using Snow.DependencyInjection;

namespace Snow.Workflows.Elsa;

/// <summary>
/// 用 Elsa 引擎实现的工作流启动器, 顶替契约包的静默实现。
/// </summary>
[TransientService(ReplaceServices = true)]
public sealed class ElsaSnowWorkflowRunner : ISnowWorkflowRunner
{
    private readonly IWorkflowRunner _runner;
    private readonly IWorkflowDefinitionService _definitions;
    private readonly IServiceProvider _services;

    /// <summary>
    /// 由容器注入。
    /// </summary>
    public ElsaSnowWorkflowRunner(IWorkflowRunner runner, IWorkflowDefinitionService definitions, IServiceProvider services)
    {
        _runner = runner;
        _definitions = definitions;
        _services = services;
    }

    /// <inheritdoc />
    public async Task<SnowWorkflowResult> StartAsync(string definitionId, IDictionary<string, object?>? input = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(definitionId);

        WorkflowDefinitionHandle handle = WorkflowDefinitionHandle.ByDefinitionId(definitionId, VersionOptions.Published);
        WorkflowGraph? graph = await _definitions.FindWorkflowGraphAsync(handle, cancellationToken).ConfigureAwait(false);
        if (graph is null)
        {
            throw new SnowWorkflowException(definitionId, "找不到已发布的工作流定义: " + definitionId);
        }

        RunWorkflowResult result = await _runner.RunAsync(graph, BuildOptions(input), cancellationToken).ConfigureAwait(false);
        return Map(result);
    }

    /// <inheritdoc />
    public async Task<SnowWorkflowResult> StartAsync(Type workflowType, IDictionary<string, object?>? input = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workflowType);

        object instance = _services.GetService(workflowType) ?? Activator.CreateInstance(workflowType)
            ?? throw new SnowWorkflowException(workflowType.FullName ?? workflowType.Name, "无法创建工作流实例: " + workflowType.FullName);

        RunWorkflowResult result;
        if (instance is IWorkflow workflow)
        {
            result = await _runner.RunAsync(workflow, BuildOptions(input), cancellationToken).ConfigureAwait(false);
        }
        else if (instance is IActivity activity)
        {
            result = await _runner.RunAsync(activity, BuildOptions(input), cancellationToken).ConfigureAwait(false);
        }
        else
        {
            throw new SnowWorkflowException(workflowType.FullName ?? workflowType.Name, "类型不是可执行的 Elsa 工作流: " + workflowType.FullName);
        }

        return Map(result);
    }

    private static RunWorkflowOptions BuildOptions(IDictionary<string, object?>? input)
    {
        RunWorkflowOptions options = new();
        if (input is { Count: > 0 })
        {
            Dictionary<string, object> values = new(StringComparer.Ordinal);
            foreach (KeyValuePair<string, object?> pair in input)
            {
                if (pair.Value is not null)
                {
                    values[pair.Key] = pair.Value;
                }
            }

            options.Input = values;
        }

        return options;
    }

    private static SnowWorkflowResult Map(RunWorkflowResult result)
    {
        WorkflowExecutionContext context = result.WorkflowExecutionContext;

        return new SnowWorkflowResult
        {
            WorkflowInstanceId = context.Id,
            Status = context.Status.ToString(),
            SubStatus = context.SubStatus.ToString(),
            Incidents = context.Incidents.Select(incident => incident.Message).ToArray(),
            Output = new Dictionary<string, object?>(context.Output, StringComparer.Ordinal),
        };
    }
}

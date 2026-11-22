using Snow.Modularity;

namespace Snow.Workflows;

/// <summary>
/// 工作流契约模块: 空锚点。加载它只是把本程序集纳入约定扫描(让静默实现被登记),
/// 真正的引擎由实现包(如 Snow.Workflows.Elsa)在各自模块里接入。
/// </summary>
public sealed class SnowWorkflowsModule : SnowModule;

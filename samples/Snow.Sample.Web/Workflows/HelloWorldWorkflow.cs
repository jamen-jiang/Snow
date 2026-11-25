using Elsa.Workflows;
using Elsa.Workflows.Activities;

namespace Snow.Sample.Web.Workflows;

/// <summary>
/// 代码方式定义的工作流示例: 一个入口, 顺序执行两步(打印一句、再打印一句)。
/// <para>通过 <see cref="SnowElsaOptions.WorkflowTypes"/> 登记后, 模块用 <c>services.AddWorkflow(type)</c>
/// 把它注册成 Elsa 工作流定义; 面板与管理 API 里按定义名(默认取类名)启动即可。</para>
/// <para>Elsa 的工作流定义就是"用 <see cref="IWorkflowBuilder"/> 搭一棵活动树": 这里用
/// <see cref="Sequence"/> 串起两个 <see cref="WriteLine"/>, 换成 <c>If</c> / <c>ForEach</c> / HTTP 活动同理。</para>
/// </summary>
public sealed class HelloWorldWorkflow : WorkflowBase
{
    /// <inheritdoc />
    protected override void Build(IWorkflowBuilder builder)
    {
        builder.Root = new Sequence
        {
            Activities =
            {
                new WriteLine("Hello World, 来自 Snow.Sample.Web 的代码工作流"),
                new WriteLine("第二步: 工作流跑完了"),
            },
        };
    }
}

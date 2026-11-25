using Elsa.Features.Services;

namespace Snow.Workflows.Elsa;

/// <summary>
/// Elsa 接入选项。
/// <para>模块不读配置节(与 ABP 一致): 宿主在启动模块的 <c>PreConfigureServices</c> 里
/// <c>Configure&lt;SnowElsaOptions&gt;(o =&gt; ...)</c> 给出; 本节示例宿主另按 <c>Workflows:Elsa</c>
/// 节绑一次标量, 那是宿主的自由, 不是模块的要求。</para>
/// <para>够不够用就一把梭: 换数据库提供程序、挂身份、改 BaseUrl 这类细调都走 <see cref="ConfigureElsa"/>,
/// 它在模块内置配置之后执行, 相当于追加一层回调。</para>
/// </summary>
public sealed class SnowElsaOptions
{
    /// <summary>
    /// SQLite 连接串。非空即启用持久化(定义与实例落库); 留空则纯内存, 进程重启即丢。
    /// <para>包内只带 SQLite 提供程序; 换 SQL Server / PostgreSQL 用 <see cref="ConfigureElsa"/>
    /// 里的 <c>UseWorkflowManagement</c> / <c>UseWorkflowRuntime</c> 换掉。</para>
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// 是否挂载 Elsa 的 HTTP 端点(工作流可被 URL 触发)。关掉时管理 API 仍在, 宿主也不用调
    /// <c>app.UseSnowWorkflows()</c>。
    /// </summary>
    public bool EnableHttp { get; set; } = true;

    /// <summary>
    /// HTTP 端点基路径。
    /// </summary>
    public string HttpBasePath { get; set; } = "/workflows";

    /// <summary>
    /// 是否挂载 Elsa 的管理 API(/elsa/api)。
    /// </summary>
    public bool EnableApi { get; set; } = true;

    /// <summary>
    /// 要注册成工作流定义的代码类型集合。
    /// </summary>
    public IList<Type> WorkflowTypes { get; } = new List<Type>();

    /// <summary>
    /// 最后一道口子: 直接拿到 Elsa 的模块对象, 想加什么特性都行
    /// <para>它在模块内置的表达式 / 缓存 / 持久化 / HTTP / 管理 API 配置之后执行, 所以能改这些内置项;
    /// 又因为 Elsa 的特性配置是追加式回调, 同一特性在这里再调一次相当于"内置之后补一刀"。</para>
    /// </summary>
    public Action<IModule>? ConfigureElsa { get; set; }
}

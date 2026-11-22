using Elsa.Features.Services;
using Elsa.Workflows.Management.Features;
using Elsa.Workflows.Runtime.Features;

namespace Snow.Workflows.Elsa;

/// <summary>
/// Elsa 使用的数据库提供程序。
/// </summary>
public enum SnowElsaDatabaseProvider
{
    /// <summary>
    /// 不配持久化: 定义与实例都在内存里, 进程重启即丢。适合试跑与单元测试。
    /// </summary>
    None = 0,

    /// <summary>
    /// SQLite(Snow.Workflows.Elsa 已内置该提供程序)。
    /// </summary>
    Sqlite = 1,
}

/// <summary>
/// Elsa 接入选项。既可通过配置节 Workflows:Elsa 绑定标量, 也可在 AddApplication 之前
/// 用 services.AddSnowElsa(o =&gt; ...) 追加代码级设置(工作流类型、数据库回调等)。
/// </summary>
public sealed class SnowElsaOptions
{
    /// <summary>
    /// 数据库连接串。非空且 Database 为 Sqlite 时启用持久化; 为空则纯内存。
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// 使用的数据库提供程序, 默认 None。
    /// </summary>
    public SnowElsaDatabaseProvider Database { get; set; } = SnowElsaDatabaseProvider.None;

    /// <summary>
    /// 启动时是否自动迁移数据库结构。Elsa 的迁移由宿主启动阶段的服务执行,
    /// 因此只有在真正的 ASP.NET 宿主里才生效(单纯构建 ServiceProvider 不会触发)。
    /// </summary>
    public bool RunMigrations { get; set; } = true;

    /// <summary>
    /// 是否挂载 Elsa 的 HTTP 端点(工作流可被 URL 触发)。
    /// </summary>
    public bool EnableHttp { get; set; } = true;

    /// <summary>
    /// HTTP 端点基路径。
    /// </summary>
    public string HttpBasePath { get; set; } = "/workflows";

    /// <summary>
    /// HTTP 端点对外基地址(留空则由请求推断)。
    /// </summary>
    public Uri? HttpBaseUrl { get; set; }

    /// <summary>
    /// 是否挂载 Elsa 的管理 API(/elsa/api)。
    /// </summary>
    public bool EnableApi { get; set; } = true;

    /// <summary>
    /// 要注册成工作流定义的代码类型集合。
    /// </summary>
    public IList<Type> WorkflowTypes { get; } = new List<Type>();

    /// <summary>
    /// 细调管理端的持久化特性(比如换成 SQL Server 提供程序)。在对 Elsa 的
    /// UseWorkflowManagement 回调里、内置配置之后调用。
    /// </summary>
    public Action<WorkflowManagementFeature>? ConfigureWorkflowManagement { get; set; }

    /// <summary>
    /// 细调运行时的持久化特性。
    /// </summary>
    public Action<WorkflowRuntimeFeature>? ConfigureWorkflowRuntime { get; set; }

    /// <summary>
    /// 最后一道口子: 直接拿到 Elsa 的模块对象, 想加什么特性都行。
    /// </summary>
    public Action<IModule>? ConfigureElsa { get; set; }
}

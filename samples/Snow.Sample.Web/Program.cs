// 最小可运行 Web 宿主示例: 一个启动模块 + 一条端点, 展示 Snow 的启动流程。
// 运行:  dotnet run --project samples/Snow.Sample.Web
using Elsa.Extensions;
using Hangfire.Storage.SQLite;
using Medallion.Threading.FileSystem;
using Snow.Sample.Web;
using Snow.Workflows.Elsa;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Hangfire 持久化存储: 用框架的 AddSnowHangfire 登记(兼容级别/序列化器/序列化设置这些样板已内置),
// 只需在回调里指定存储。换库换包与 UseXxxStorage 即可: Hangfire.SqlServer / Hangfire.PostgreSql / Hangfire.MySql。
// 注意: SQLite 提供程序把这个参数当"数据库文件名"; 换成关系库时填真实连接串。
// 路径相对宿主的内容根, 配成 DataBase/xxx.db 就落在本项目目录下的 DataBase/ 里。
string hangfireConnectionString = builder.Configuration.GetConnectionString("Hangfire")
    ?? throw new InvalidOperationException("缺少配置 ConnectionStrings:Hangfire: Hangfire 需要持久化存储的位置(文件路径或连接串)。");
builder.Services.AddSnowHangfire(configuration =>
    configuration.UseSQLiteStorage(ResolveSqlitePath(hangfireConnectionString, builder.Environment.ContentRootPath)));

// SnowElsaModule 已内置运行工作流所需的最小特性集(表达式、JS 引擎、内存缓存、管理与运行时的缓存),
// 宿主只需补取舍项。这里挂的是身份: 仅开发环境播种一个开发管理员, 免去「无用户可登录」的启动诊断。
builder.Services.AddSnowElsa(options =>
{
    options.ConfigureElsa = elsa => elsa.UseIdentity(identity => identity.UseDevelopmentAdminUserProvider());

    // Elsa 的分布式锁默认用文件锁, 目录写死在内容根下的 App_Data/locks, 每次启动都重建。
    // 这里把根目录换成 DataBase/locks: 与 Hangfire 的库放一起, 一眼看得出哪些是运行时产物。
    options.ConfigureWorkflowRuntime = runtime =>
    {
        string lockDirectory = Path.Combine(builder.Environment.ContentRootPath, "DataBase", "locks");
        Directory.CreateDirectory(lockDirectory);
        runtime.DistributedLockProvider = _ => new FileDistributedSynchronizationProvider(new DirectoryInfo(lockDirectory));
    };
});

// 装配启动模块及其全部依赖, 并立即执行服务注册阶段
await builder.AddApplicationAsync<SampleModule>();

WebApplication app = builder.Build();

// 驱动模块生命周期(必须 Build 之后、Run 之前调用一次)
await app.InitializeApplicationAsync();

// 端点由宿主映射(框架不代挂骨架, 与 ABP 模板风格一致)。
// 认证模块把全局回退策略设为「必须已登录」, 所以公开探针端点显式 AllowAnonymous。
app.MapGet("/", () => "Hello Snow!").AllowAnonymous();
app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();

await app.RunAsync();

// SQLite 存储把参数当文件名, 而 "Data Source=" 形态是给关系库用的: 两种都认, 相对路径按内容根展开,
// 并把目录建出来 —— 否则 SQLite 只在进程工作目录下找文件, 换个启动方式就换了一个库。
static string ResolveSqlitePath(string connectionString, string contentRootPath)
{
    foreach (string key in new[] { "Data Source", "Filename" })
    {
        string prefix = key + "=";
        int index = connectionString.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
        {
            continue;
        }

        int start = index + prefix.Length;
        int end = connectionString.IndexOf(';', start);
        string value = (end < 0 ? connectionString[start..] : connectionString[start..end]).Trim();
        return connectionString[..start] + ExpandPath(value, contentRootPath) + (end < 0 ? string.Empty : connectionString[end..]);
    }

    return ExpandPath(connectionString.Trim(), contentRootPath);
}

static string ExpandPath(string path, string contentRootPath)
{
    if (path.Length == 0 || path == ":memory:" || Path.IsPathRooted(path))
    {
        return path;
    }

    string fullPath = Path.GetFullPath(Path.Combine(contentRootPath, path));
    string? directory = Path.GetDirectoryName(fullPath);
    if (!string.IsNullOrEmpty(directory))
    {
        Directory.CreateDirectory(directory);
    }

    return fullPath;
}
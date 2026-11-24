// 最小可运行 Web 宿主示例: 一个启动模块 + 一条端点, 展示 Snow 的启动流程。
// 运行:  dotnet run --project samples/Snow.Sample.Web
using Elsa.Extensions;
using Hangfire.Storage.SQLite;
using Snow.Sample.Web;
using Snow.Workflows.Elsa;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Hangfire 持久化存储: 用框架的 AddSnowHangfire 登记(兼容级别/序列化器/序列化设置这些样板已内置),
// 只需在回调里指定存储。换库换包与 UseXxxStorage 即可: Hangfire.SqlServer / Hangfire.PostgreSql / Hangfire.MySql。
// 注意: SQLite 提供程序把这个参数当"数据库文件名"; 换成关系库时填真实连接串。
string hangfireConnectionString = builder.Configuration.GetConnectionString("Hangfire")
    ?? "snow-hangfire.db";
builder.Services.AddSnowHangfire(configuration => configuration.UseSQLiteStorage(hangfireConnectionString));

// SnowElsaModule 已内置运行工作流所需的最小特性集(表达式、JS 引擎、内存缓存、管理与运行时的缓存),
// 宿主只需补取舍项。这里挂的是身份: 仅开发环境播种一个开发管理员, 免去「无用户可登录」的启动诊断。
builder.Services.AddSnowElsa(options =>
{
    options.ConfigureElsa = elsa => elsa.UseIdentity(identity => identity.UseDevelopmentAdminUserProvider());
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
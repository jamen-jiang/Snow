// 最小可运行 Web 宿主示例: 一个启动模块 + 一条端点, 展示 Snow 的启动流程。
// 运行:  dotnet run --project samples/Snow.Sample.Web
using Snow.Sample.Web;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

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

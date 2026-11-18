// 宿主入口:创建 WebApplication 构建器,由启动模块驱动各模块的服务注册
using Microsoft.Extensions.DependencyInjection;
using Rongia;
using Rongia.Modularity;

var builder = WebApplication.CreateBuilder(args);

// 模块的 ServiceConfigurationContext.Configuration 直接从服务集合里取配置对象,
// 所以要在装配模块之前把宿主的 IConfiguration 以实例形式登记进来
builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

// 解析启动模块的全部依赖,并立即执行模块的服务注册阶段
IApplication application = await ApplicationFactory.CreateAsync<RongiaModule>(builder.Services);

var app = builder.Build();

// 容器已构建完成,驱动模块的应用初始化阶段
await application.InitializeAsync(app.Services);

// 进程停止时按模块加载顺序的逆序执行关闭阶段,此时容器尚未释放
app.Lifetime.ApplicationStopping.Register(() => application.ShutdownAsync(app.Services).GetAwaiter().GetResult());

// 健康检查端点,供容器编排与负载均衡探活
app.MapGet("/health", () => Results.Ok(new { status = "up" }));

app.Run();

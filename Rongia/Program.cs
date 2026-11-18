// 宿主入口:创建 WebApplication 构建器,由启动模块驱动各模块的服务注册
using Rongia;
using Rongia.Modularity;

var builder = WebApplication.CreateBuilder(args);

// 解析启动模块的全部依赖,并立即执行模块的服务注册阶段
builder.Services.AddApplication<RongiaModule>();

var app = builder.Build();

// 容器已构建完成,驱动模块的应用初始化阶段
app.Services.Initialize();

// 进程停止时按模块加载顺序的逆序执行关闭阶段,此时容器尚未释放
app.Lifetime.ApplicationStopping.Register(() => app.Services.Shutdown());

// 健康检查端点,供容器编排与负载均衡探活
app.MapGet("/health", () => Results.Ok(new { status = "up" }));

app.Run();

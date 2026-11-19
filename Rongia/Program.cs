// 宿主入口: 创建 WebApplication 构建器, 由启动模块驱动各模块的服务注册与初始化
using Microsoft.Extensions.DependencyInjection;
using Rongia;
using Rongia.Modularity;

var builder = WebApplication.CreateBuilder(args);

// 模块装配阶段要从容器取到宿主的配置对象, 故先以实例形式登记
builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

// 加载启动模块及其全部依赖, 完成各模块的服务注册阶段
builder.Services.AddApplication<RongiaModule>();

var app = builder.Build();

// 容器构建完成, 驱动各模块的应用初始化阶段
app.Services.Initialize();

// 进程停止时按模块加载顺序的逆序执行关闭阶段, 此时容器尚未释放
app.Lifetime.ApplicationStopping.Register(() => app.Services.Shutdown());

// 认证/授权中间件: 默认认证方案为 SaToken(不透明会话令牌, 状态存 Redis, 每次校验滑动续期)
app.UseAuthentication();
app.UseAuthorization();

// 健康检查端点, 供容器编排与负载均衡探活(全站默认要求登录, 故此处显式匿名)
app.MapGet("/health", () => Results.Ok(new { status = "up" })).AllowAnonymous();

app.Run();

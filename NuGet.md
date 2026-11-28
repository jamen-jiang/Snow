# Snow Framework

Snow 是一套 .NET 11 的模块化应用框架, 对标 ABP Framework: 模块系统、约定式依赖注入、AOP 拦截、
事件总线、命令/查询派发、工作单元与事务、EF Core 仓储、表达式引擎、分布式缓存与分布式锁。

所有包同版本发布, 版本号由仓库根目录的 `common.props` 统一维护。
仓库按目录分流: 框架代码在 `src/`, 测试在 `test/`, 两者在解决方案里各占一个文件夹。

## 安装

```bash
# Web 宿主: 一次拿到 ASP.NET Core 场景下的全部框架能力
dotnet add package Snow.AspNetCore

# 数据库提供程序按需再加一个
dotnet add package Snow.EntityFrameworkCore.SqlServer
dotnet add package Snow.EntityFrameworkCore.Sqlite
dotnet add package Snow.EntityFrameworkCore.MySql
dotnet add package Snow.EntityFrameworkCore.PostgreSql
```

## 快速开始

```csharp
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// 加载启动模块及其全部依赖, 并立即执行服务注册阶段。
// Web 宿主用 builder 上的这个入口: 它顺手把 builder.Configuration 顶进容器、
// 把 builder.Environment.EnvironmentName 抄进应用创建选项 —— 宿主不必再写
// AddSingleton<IConfiguration>(builder.Configuration), 也不必自己填环境名。
await builder.AddApplicationAsync<MyModule>();

WebApplication app = builder.Build();

// 初始化应用: 填好对象访问器、挂上关停钩子、驱动全部模块的初始化阶段
// 管道由启动模块自己在 OnApplicationInitialization 里写, 这里不写任何 UseXxx
await app.InitializeApplicationAsync();

app.Run();
```

模块类继承 `SnowModule`(或只实现 `ISnowModule` 接口)并用 `[DependsOn]` 声明依赖:

```csharp
[DependsOn(
    typeof(SnowAspNetCoreModule),
    typeof(SnowEntityFrameworkCoreSqlServerModule))]
public sealed class MyModule : SnowModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.Configure<SnowDbContextOptions>(options => options.UseSqlServer());
    }
}
```

换数据库只改这一行: `options.UseSqlServer()` / `UseSqlite()` / `UseMySql()` / `UsePostgreSql()`, 连接串都从 `ConnectionStrings:{名称}` 读。

业务类型标一个生命周期注解就参与约定注册: `[SingletonService]`、`[ScopedService]`、`[TransientService]`,
要顶替既有实现再加 `(ReplaceServices = true)`; 也可以直接写 `[Service(ServiceLifetime.Transient)]`。
没有默认生命周期, 标注处一眼看出这个服务活多久。
接口名与实现类名去掉 `I ` 前缀一致即自动暴露为该接口的实现。注解可继承, 标在抽象基类上子类也生效。

## 模块生命周期: 六个可重写的阶段

模块基类照着 ABP 的 `AbpModule` 给: 服务注册三趟, 应用初始化三段, 加一个关停。每个阶段都有同步与异步一对, 异步版默认调用同步版 —— 重写任意一个都生效。
每个阶段另有一个接口(`IPreConfigureServices` / `IPostConfigureServices` / `IOnPreApplicationInitialization` / `IOnApplicationInitialization` / `IOnPostApplicationInitialization` / `IOnApplicationShutdown`), `SnowModule` 的类声明把它们全部列上, 框架按接口判断再调用 —— 与 `AbpModule` 的声明逐项对齐。

`ISnowModule` 与 ABP 的 `IAbpModule` 成员逐个对齐, **只有服务注册中段那两个方法**:

```csharp
public interface ISnowModule
{
    Task ConfigureServicesAsync(ServiceConfigurationContext context);
    void ConfigureServices(ServiceConfigurationContext context);
}
```

所以「是不是模块」只看这个接口(`SnowModuleHelper.CheckModuleType` 判 `typeof(ISnowModule).IsAssignableFrom`), 启动模块的泛型约束是 `where TStartupModule : ISnowModule`, 不要求继承 `SnowModule`。前段与后段在 ABP 里本来就是独立接口, 这里同理: 想参与它们就实现对应接口, 不参与也不必被迫继承基类。

| 阶段 | 方法 | 何时执行 | 能做什么 |
| --- | --- | --- | --- |
| 服务注册前段 | `PreConfigureServices` / `…Async` | `AddApplication` 内部, 全部模块各跑一趟 | 必须赶在别人注册之前的占位登记 |
| 服务注册中段 | `ConfigureServices` / `…Async` | 约定扫描之后, 全部模块各跑一趟 | `context.Services` 登记服务; 此时容器还没建, 不要解析服务 |
| 服务注册后段 | `PostConfigureServices` / `…Async` | 全部中段跑完之后, 全部模块各跑一趟 | 看到依赖模块登记了什么之后再替换默认实现、补装饰器 |
| 预初始化 | `OnPreApplicationInitialization` / `…Async` | 容器已构建, 管道还没开始铺 | 最早的一次性接线(对象访问器填值、平台探针) |
| 第一段 | `OnApplicationInitialization` / `…Async` | 容器已构建, 管道由宿主在这段里写 | 订阅事件、预热缓存; Web 宿主在这里用 `context.GetApplicationBuilder()` 手写整条管道(见「中间件管道」) |
| 收尾段 | `OnPostApplicationInitialization` / `…Async` | 全部模块的第一段跑完之后 | 需要"所有模块都初始化完"再做的收尾动作(管道此刻已由宿主铺好, 不再靠这两个阶段区分位置) |
| 关停 | `OnApplicationShutdown` / `…Async` | `IHostApplicationLifetime.ApplicationStopping`(Web)或 `serviceProvider.Shutdown()`(非 Web) | 冲刷缓冲、注销注册、停自建的后台循环; 容器还没释放 |

- 服务注册是**三趟**, 不是"一个模块跑完三段再下一个": 全部模块的前段跑完, 再全部跑中段, 最后全部跑后段(与 ABP 一致)。所以前段里能保证"别人还没登记", 后段里能保证"别人都登记完了"。
- 依赖声明只有 `[DependsOn(typeof(XModule))]`, 拓扑排序后保证被依赖的模块先注册、先初始化; 同一趟内的先后也按这个顺序。
- 模块实例本身以自身类型登记成单例, 所以模块里可以写构造注入。
- `ServiceConfigurationContext` 只在服务注册的三趟里可用, 在初始化阶段访问会抛 `SnowInitializationException`。
- 三个阶段由宿主显式触发(对齐 ABP): Web 宿主在 `builder.Build()` 之后调一次 `await app.InitializeApplicationAsync()`, 它填好四个 Web 对象访问器、把关停挂到 `ApplicationStopping`、再按"预初始化 → 第一段 → 收尾段"跑完, 启动模块就在其中的第一段里写管道; 非 Web 宿主(控制台 / Worker)调 `await serviceProvider.InitializeAsync()` 一次把三段跑完(也可以 `PreInitializeModulesAsync()` / `InitializeModulesAsync()` / `PostInitializeModulesAsync()` 分开跑), 退出前 `serviceProvider.Shutdown()`。框架不注册 `IStartupFilter`, `builder.Build()` 里不会偷偷初始化。
- 中间件由宿主在启动模块的第一段里手写(`context.GetApplicationBuilder()` 拿构建器, 按行序 `app.UseXxx()`), 能力包只提供 `UseXxx()` 扩展方法; 不需要 `Order`、不登记、不实现接口 —— 位置就是代码行序(见下文「中间件管道」)。
- 关停阶段的服务解析走独立作用域, 所以关停里可以正常注入 Scoped 服务; 重复调用幂等。
- 需要「在 `AddApplication` 之前」生效的选项(`AddSnowOpenApi` / `AddSnowRateLimiting` 等), 直接在 `builder.Services` 上调用, 不要塞进模块的 `ConfigureServices` —— 那时被依赖模块可能已经跑过, 动作就晚了。

## Web 宿主: 模块只做接线

`Snow.AspNetCore` 只有一件事: **Web 接线**(职责对位 ABP 的 `AbpAspNetCoreModule`)。它只引用接线本身跑得起来的 6 个基础模块 —— 审计 / 安全 / 授权 / 工作单元 / 时间 / 链路标识。
除此之外的能力都不在本包里: 应用层、认证、缓存、分布式锁、后台作业、事件总线、消息、邮件、FTP、HTTP 客户端、Excel、工作流、EntityFrameworkCore、数据过滤(`Snow.Data`)、AOP(`Snow.Castle`) 以及它们的第三方实现, 由宿主按需引用。ABP 也是这个做法: `Volo.Abp.AspNetCore` 只依赖 9 个基础模块, Caching / EventBus / BackgroundJobs 全部留给宿主声明。
所以本包只有 6 个包引用, 没有一个是第三方依赖。

**包引用不等于模块加载。** `SnowAspNetCoreModule` 只自动装配它依赖的 6 个基础模块。缓存、分布式锁、后台作业、事件总线、消息、邮件、FTP、HTTP 客户端这些要自己引用包并声明依赖:

```csharp
[DependsOn(
    typeof(SnowAspNetCoreModule),
    typeof(SnowCachingModule),
    typeof(SnowEventBusModule))]
public sealed class MyHttpApiHostModule : SnowModule { }
```

这样加载图由宿主决定, 不会因为引了 Web 包就把没用到的能力全拉起来。

| 做什么 | 说明 |
| --- | --- |
| 上下文访问器 | 登记 `IHttpContextAccessor`, 当前用户与当前请求都从这里来 |
| 对象访问器 | `IObjectAccessor<IApplicationBuilder>` / `<WebApplication>` / `<IHost>` / `<IEndpointRouteBuilder>`, 让单例服务也能拿到只有宿主才有的对象 |
| 环境与声明 | `GetHostingEnvironment()` 取宿主 Web 环境(没登记就返回环境名为 `Development` 的空实现, 不必自己写 `??` 分支) |
| 宿主环境名 | `ISnowHostEnvironment` 在模块装配前就登记好(默认 `SnowHostEnvironment`), 模块在服务注册阶段就能问「现在是什么环境」, 不必等容器建好 |

### 宿主环境名: `ISnowHostEnvironment`

`IWebHostEnvironment` 只有 Web 宿主才有, 而模块在**服务注册阶段**就要按环境分支 —— 那时容器还没建, 也不该为了一个字符串让内核包去依赖 ASP.NET Core。所以环境名单独抽一层, 与 ABP 的 `IAbpHostEnvironment` 逐项对应:

| 类型 | 位置 | 对应 ABP |
| --- | --- | --- |
| `ISnowHostEnvironment`(只有 `string? EnvironmentName { get; set; }`) | `Snow.Core`, 命名空间 `Snow` | `IAbpHostEnvironment` |
| `SnowHostEnvironment`(唯一实现, 可写) | `Snow.Core` | `AbpHostEnvironment` |
| `SnowHostEnvironmentExtensions`(`IsDevelopment` / `IsStaging` / `IsProduction` / `IsEnvironment(string)`, 比较忽略大小写) | `Snow.Core` | `AbpHostEnvironmentExtensions` |
| `SnowEnvironments`(`Development` / `Staging` / `Production` 三个常量) | `Snow.Core` | `Microsoft.Extensions.Hosting.Environments`(值相同, 只是不为一个常量引 `Hosting.Abstractions`) |

填充顺序与 ABP 一致, 三处各管一段:

1. `AddApplication` / `AddApplicationAsync` 在**装配模块之前**用 `TryAddSingleton<ISnowHostEnvironment>(new SnowHostEnvironment { EnvironmentName = options.Environment })` 放好可写单例 —— 宿主若先放了自己的实现就不覆盖。对应 ABP 在 `AbpApplicationBase` 构造器里 `services.AddSingleton<IAbpHostEnvironment>(new AbpHostEnvironment { EnvironmentName = options.Environment })`; `options` 就是 `SnowApplicationCreationOptions`。
2. `SnowAspNetCoreModule.PreConfigureServices` 在环境名为空时取 `context.Services.GetHostingEnvironment().EnvironmentName` 填进去 —— Web 宿主下就是 `ASPNETCORE_ENVIRONMENT` 的值; 非 Web 宿主(或还没被 Web 主机登记)时 `GetHostingEnvironment()` 给出环境名为 `Development` 的空实现, 所以这一句与 ABP 的 `AbpAspNetCoreModule` 完全同形, 不会留下空环境名。
3. 服务注册收尾(`PostConfigureServices` 全部跑完之后)兜底: 全程没人填就置成 `Production`。对应 ABP 的 `AbpApplicationBase.TryToSetEnvironment`。

```csharp
public sealed class MyModule : SnowModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        // 服务注册阶段就能判断环境, 此时容器还没建
        ISnowHostEnvironment host = context.Services.GetSingletonInstance<ISnowHostEnvironment>();
        if (host.IsDevelopment())
        {
            context.Services.AddSingleton<ICache, InMemoryCache>();
        }
    }
}
```

服务注册之后(初始化阶段、请求期)直接用容器里的 `ISnowHostEnvironment`, 它是单例; 宿主想自己说了算就在 `AddApplication` 之前 `services.AddSingleton<ISnowHostEnvironment>(new SnowHostEnvironment { EnvironmentName = "Staging" })`。

### Web 宿主入口: `builder.AddApplicationAsync<T>()`

对应 ABP 的 `WebApplicationBuilderExtensions`(同名类、同在 `Microsoft.Extensions.DependencyInjection` 命名空间, 宿主不必额外 using)。它比 `builder.Services.AddApplicationAsync<T>()` 多的就两件事, 与 ABP 逐行对应:

```csharp
public static async Task<SnowApplication> AddApplicationAsync<TStartupModule>(
    this WebApplicationBuilder builder,
    Action<SnowApplicationCreationOptions>? optionsAction = null)
    where TStartupModule : ISnowModule
{
    return await builder.Services.AddApplicationAsync<TStartupModule>(options =>
    {
        options.Services.ReplaceConfiguration(builder.Configuration);   // 配置顶进容器
        optionsAction?.Invoke(options);
        if (options.Environment.IsNullOrWhiteSpace())
        {
            options.Environment = builder.Environment.EnvironmentName;  // 环境名抄进选项
        }
    });
}
```

| 成员 | 落点 | 作用 | 对应 ABP |
| --- | --- | --- | --- |
| `AddApplicationAsync<TStartupModule>(builder, optionsAction)` | `Snow.AspNetCore`, 命名空间 `Microsoft.Extensions.DependencyInjection` | Web 宿主装配入口 | `WebApplicationBuilderExtensions.AddApplicationAsync<TStartupModule>` |
| `AddApplicationAsync(builder, startupModuleType, optionsAction)` | 同上 | 按类型装配 | 同上的类型重载 |
| `SnowApplicationCreationOptions`(`Services` / `Environment`) | `Snow.Core`, 命名空间 `Snow.Modularity` | 应用创建选项 | `AbpApplicationCreationOptions` |
| `ReplaceConfiguration(services, configuration)` | `Snow.Core`, 命名空间 `Snow.DependencyInjection` | 用 `Replace` 顶掉已有的 `IConfiguration` 登记 | `ServiceCollectionConfigurationExtensions.ReplaceConfiguration` |

ABP 那份创建选项还有 `PlugInSources` / `Configuration` / `SkipConfigureServices` / `ApplicationName` 四项, 分别服务于插件源、自建配置、跳过服务注册、应用信息访问器 —— 这几个子系统 Snow 还没有, 所以只留真正被消费的 `Environment`: 少一个成员比多一个没人读的成员好, 等子系统落地再往里加。

宿主想自己说了算, 三条路任选: `optionsAction` 里填 `Environment`(优先于 `builder.Environment`)、在 `AddApplicationAsync` 之前 `services.AddSingleton<ISnowHostEnvironment>(...)`(`TryAddSingleton` 不覆盖)、或者根本不用这个入口, 直接 `builder.Services.AddApplicationAsync<T>()` 并自己 `AddSingleton<IConfiguration>`。

接线: 启动模块声明 `[DependsOn(typeof(SnowAspNetCoreModule))]`, 并在它的 `OnApplicationInitialization` 里手写管道(见「中间件管道」); `Program.cs` 里仍然只有 `builder.Build()` 与 `app.Run()`。
宿主在 `builder.Build()` 之后调一次 `await app.InitializeApplicationAsync()`: 填好四个 Web 对象访问器, 驱动全部模块的应用初始化(宿主就在第一段里写管道), 跑收尾段, 并把模块的关停阶段挂到 `ApplicationStopping` 上。端点映射由宿主最后一行 `app.UseSnowEndpoints()` 触发。

`InitializeApplicationAsync()` 替你保证: 对象访问器一定填好、模块初始化一定按拓扑序跑完、关停在 `ApplicationStopping` 上只跑一次、重复调用幂等 —— 这些不用宿主记。管道本身则写在你的启动模块里, 与 ABP 模板的 `await app.InitializeApplicationAsync()` 逐行对应。

模块在 `OnApplicationInitialization` 里要拿宿主对象, 用 `ApplicationInitializationContext` 上的扩展(与 ABP 的 `ApplicationInitializationContextExtensions` 一一对应):

| 扩展方法 | 取不到时 | 对应 ABP | 什么时候用 |
| --- | --- | --- | --- |
| `GetApplicationBuilder()` / `GetApplicationBuilderOrNull()` | 抛 `SnowInitializationException` / 返回 null | 同名 | 在启动模块里手写管道 |

带 `OrNull` 的版本是给非 Web 宿主用的: 控制台 / Worker 里 `GetApplicationBuilderOrNull()` 返回 null, 模块据此跳过 Web 相关的接线, 而不是抛异常。


```csharp
// 单例里取当前应用对象(只读用途: 看环境、读配置、拿根容器)
public sealed class MyService(IObjectAccessor<IApplicationBuilder> accessor)
{
    public IServiceProvider? RootServices => accessor.Value?.ApplicationServices;
}
```

### 宿主环境: `GetHostingEnvironment()`

这个扩展在 `SnowAspNetCoreServiceCollectionExtensions`(命名空间 `Microsoft.Extensions.DependencyInjection`, 与 ABP 的 `AbpAspNetCoreServiceCollectionExtensions` 同位置), 只在需要时调:

```csharp
// 服务注册阶段取宿主环境: 没登记 IWebHostEnvironment 时返回环境名为 Development 的空实现
IWebHostEnvironment environment = builder.Services.GetHostingEnvironment();
if (environment.EnvironmentName == SnowEnvironments.Development)
{
    builder.Services.AddSingleton<ICache, InMemoryCache>();
}

```

- `GetHostingEnvironment()` 与 `ISnowHostEnvironment` 的分工: 后者是给**模块在服务注册阶段**用的可写环境名(内核层, 不依赖 ASP.NET Core); 前者是给宿主在 `Program.cs` 里用的只读 `IWebHostEnvironment`(要路径、要文件提供器时用它)。

### 端点: 宿主管道最后一行 `UseSnowEndpoints()`

映射端点要的是 `IEndpointRouteBuilder`, 模块在服务注册阶段拿不到它 —— 模块只把映射动作交进 `SnowEndpointRouterOptions.EndpointConfigureActions`(与 ABP 的 `AbpEndpointRouterOptions` 同形, 连命名空间都一样: `Microsoft.AspNetCore.Routing`), 由宿主在管道最后一行 `app.UseSnowEndpoints()` 统一执行:

```csharp
WebApplication app = builder.Build();
app.Run();   // 端点由启动模块最后一行 app.UseSnowEndpoints() 映射
```

`SnowAspNetCoreMvcModule` 声明 `MapControllers()`, `SnowSignalRModule` 声明 Hub 映射, `SnowOpenApiModule` 声明文档端点 —— 三个模块都在自己的 `ConfigureServices` 里写一句 `Configure<SnowEndpointRouterOptions>(...)`, 宿主最后一行 `UseSnowEndpoints()` 统一执行。这就是 ABP 的 `UseConfiguredEndpoints()` 机制本身。

**端点与中间件是两件事**: 中间件由宿主在管道里按行序挂, 端点位置唯一(永远是最后), 所以模块只需要声明映射动作。宿主挂的每一层都自动落在端点之前。

需要临时加端点, 在宿主启动模块的 `ConfigureServices` 里登记一句即可:

```csharp
public override void ConfigureServices(ServiceConfigurationContext context)
{
    Configure<SnowEndpointRouterOptions>(options =>
        options.EndpointConfigureActions.Add(endpointContext => endpointContext.Endpoints.MapGet("/ping", () => "pong")));
}
```

只在单独使用某个包、且不经过统一挂载时, 才需要手写 `app.MapSnowHubs()` / `app.UseSnowOpenApi()`。

`UseSnowEndpoints()` 与 ABP 的 `UseConfiguredEndpoints()` 签名一样, 带一个可选参数 —— 想在同一个 `UseEndpoints` 里顺手加几个端点就传进来, 不必再单独写一次 `app.UseEndpoints(...)`:

```csharp
app.UseSnowEndpoints(endpoints => endpoints.MapGet("/healthz", () => "ok"));
```

一个模块动作都没登记、又没传额外动作时, 它什么都不做(与 ABP 在 `EndpointConfigureActions` 为空且 `additionalConfigurationAction` 为 null 时直接 `return app` 同义)。

### 链路标识

每个请求一个链路标识: 先看 `X-Correlation-Id` 请求头, 没有就生成一个, 并写回响应头。请求内 `ICorrelationIdProvider.Get()` 返回同一个值, 日志增强、审计、出站 HTTP 都用它把跨服务日志串起来。

它通常放管道最外层(第一行): 认证失败、授权拒绝、路由没匹配时响应都要能带回 `X-Correlation-Id`, 而这三段都在它内层。

选项不读配置节(与 ABP 的 `AbpCorrelationIdOptions` 一样由宿主给), 在启动模块的 `PreConfigureServices` 里绑一节或直接赋值:

```csharp
Configure<SnowCorrelationIdOptions>(context.Configuration.GetSection("CorrelationId"));
// 或: Configure<SnowCorrelationIdOptions>(options => { options.HttpHeaderName = "X-Correlation-Id"; options.SetResponseHeader = true; });
```

### 中间件管道

**管道由宿主自己写**: 在启动模块的 `OnApplicationInitialization` 里用 `context.GetApplicationBuilder()` 拿构建器, 按顺序写出整条 `app.UseXxx()`, 最后一行 `app.UseSnowEndpoints()` 映射端点。框架不铺骨架、不代挂中间件、不替你映射端点 —— 与 ABP 的做法一致(ABP 模板的 `MyProjectNameHttpApiHostModule.OnApplicationInitialization` 就是手写十几行 `app.UseXxx()`)。

```csharp
[DependsOn(
    typeof(SnowAspNetCoreModule),
    typeof(SnowAspNetCoreSerilogModule),
    typeof(SnowAuthenticationModule),
    typeof(SnowAspNetCoreMultiTenancyModule),
    typeof(SnowRateLimitingModule),
    typeof(SnowHangfireModule))]
public sealed class MyHttpApiHostModule : SnowModule
{
    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        IApplicationBuilder app = context.GetApplicationBuilder();

        app.UseSnowCorrelationId();            // 1  链路标识, 最外层
        app.UseSnowSerilogEnrichers();         // 2  日志上下文, 也要包住认证
        app.UseRouting();                      // 3  路由
        app.UseAuthentication();               // 4  认证
        app.UseSnowMultiTenancy();             // 5  多租户, 认证之后才读得到租户声明
        app.UseSnowRateLimiting();             // 6  限流, 路由之后才读得到端点元数据
        app.UseAuthorization();                // 7  授权
        app.UseSnowHangfireDashboard();        // 8  终端分支, 必须晚于授权
        app.UseSnowWorkflows();                // 9  终端分支
        app.UseSnowEndpoints();                // 10 端点, 永远最后
    }
}
```

- `UseSnowEndpoints()` 执行各模块在 `SnowEndpointRouterOptions.EndpointConfigureActions` 里登记的映射动作(与 ABP 的 `UseConfiguredEndpoints()` 同形); 之前挂的每一层中间件都自动落在端点之前。
- `DependsOn` 只决定模块加载与初始化顺序(被依赖的先初始化), **不再决定中间件位置** —— 位置由上面这段代码的行序直接决定, 一眼可见, 不需要 `Order`、不登记、不实现接口。
- 每个 `UseXxx()` 都是包内 `Microsoft/AspNetCore/Builder/` 目录下的扩展方法, 命名空间 `Microsoft.AspNetCore.Builder`(与 ABP 一致: 补全列表里所有 `UseXxx` 在一起)。包的 `UseXxx()` 只负责把原生中间件包一层, 不做幂等判断。
- 类的归属也照 ABP 分: `InitializeApplicationAsync()` / `InitializeApplication()` / `UseSnowCorrelationId()` 三个都在 `SnowApplicationBuilderExtensions`(对应 ABP 把 `InitializeApplicationAsync` 与 `UseCorrelationId` 一起放在 `AbpApplicationBuilderExtensions`); `UseSnowEndpoints()` 在 `SnowAspNetCoreApplicationBuilderExtensions`(对应 ABP 的 `AbpAspNetCoreApplicationBuilderExtensions.UseConfiguredEndpoints`); 其余能力包的 `UseXxx()` 各自落在自己包的 `Microsoft/AspNetCore/Builder/` 下 —— 与 ABP 的 `AbpAspNetCoreSerilogApplicationBuilderExtensions` / `AbpAspNetCoreMultiTenancyApplicationBuilderExtensions` / `AbpHangfireApplicationBuilderExtensions` 一一对应。
- 其余类型也按 ABP 的落位规则放进被扩展类型自己的命名空间, 所以 `using` 天然就有、不用额外引 `Snow.*`: `SnowEndpointRouterOptions` / `SnowEndpointRouteBuilderContext` → `Microsoft.AspNetCore.Routing`(`Snow.AspNetCore` 的 `Microsoft/AspNetCore/Routing/`); `SnowAspNetCoreServiceCollectionExtensions.GetHostingEnvironment()` → `Microsoft.Extensions.DependencyInjection`(`Snow.AspNetCore` 的 `Microsoft/Extensions/DependencyInjection/`)。
- `ActionDescriptor` 扩展方法叫 `ActionDescriptorExtensions`(类名与 ABP 完全一样, 不带 `Snow` 前缀), 在 `Snow.AspNetCore.Mvc` 的 `Microsoft/AspNetCore/Mvc/Abstractions/`(命名空间 `Microsoft.AspNetCore.Mvc.Abstractions`), 与 ABP 同包同位置(`Volo.Abp.AspNetCore.Mvc`)—— 接线包 `Snow.AspNetCore` 不能反向引用 MVC 包, 这层桥只能落在 MVC 包。五个方法与 ABP 一一对应: `AsControllerActionDescriptor()` / `GetMethodInfo()` / `GetReturnType()` / `HasObjectResult()` / `IsControllerAction()`; 它们只做取值, 与 `[DisableSnowFeatures]` 无关。落位规矩: 放进 `Microsoft.*` 命名空间的类型, 类名照抄 ABP(ABP 带 `Abp` 前缀的照旧带, 如 `SnowEndpointRouterOptions` 对 `AbpEndpointRouterOptions`; ABP 不带的也不带), 这样从 ABP 搬过来的代码一行都不用改。
- 开关判定本身在 `Snow.Core` 的 `SnowFeatureSwitches`(收 `Type?`), 不认识 MVC 类型; 用不到 MVC 的项目直接问它即可, 不必引 `Snow.AspNetCore.Mvc`。

四条顺序约束(实测出来的):

- **限流 `UseSnowRateLimiting()` 在 `UseRouting()` 之后**: 策略挂在端点元数据上, 路由之前读不到, 会静默全放行且不报错。
- **多租户 `UseSnowMultiTenancy()` 在 `UseAuthentication()` 之后**: "当前用户"出资者要读已认证主体的租户声明; 写在认证之前会静默回落到 Host 租户。
- **终端分支(仪表盘、工作流)在 `UseAuthorization()` 之后**: 命中就自己写响应、不调 `next`, 放太前会绕过授权。

日志上下文与链路标识通常放最外层: 认证失败、授权拒绝、路由没匹配时, 响应也要带回 `X-Correlation-Id` 并进日志上下文 —— 它们在内层才做得到。

有的中间件是微软自己的 `internal` 类型, 或挂法要带参数, 包的 `UseXxx()` 就把这层差异吃掉, 宿主看到的都是一行调用:

```csharp
// Snow.AspNetCore.RateLimiting: RateLimiterMiddleware 是 internal, 只能走 UseRateLimiter()
public static IApplicationBuilder UseSnowRateLimiting(this IApplicationBuilder app) { ... return app.UseRateLimiter(); }
```

### 宿主自己的中间件

直接写在上面那段里, 放在合适的位置即可; 框架没有留"再展开一次骨架"的入口, 管道只有你写的一份:

```csharp
public override void OnApplicationInitialization(ApplicationInitializationContext context)
{
    IApplicationBuilder app = context.GetApplicationBuilder();

    app.UseSnowCorrelationId();
    app.UseRouting();
    app.UseCors("MyPolicy");                // 路由后、认证前: 能读端点元数据, 也能覆盖预检与 401 响应头
    app.UseAuthentication();
    app.UseMiddleware<MyAuditMiddleware>(); // 认证后: 读得到当前用户
    app.UseAuthorization();
    app.UseSnowEndpoints();
}
```

- **CORS 就写这里**: 它要"路由之后、认证之前" —— 用全局策略 `app.UseCors("MyPolicy")`, 或在端点级用 `[EnableCors]`(此时写无参 `app.UseCors()` 让中间件按端点策略放行)。
- 中间件实现本身是原生写法: `app.UseMiddleware<T>()` 或实现 `IMiddleware`; 写在管道里的哪一行只决定位置, 不改变实现方式。
- 约定式中间件构造器注入的 Scoped 服务实际只建一次(启动期); 需要每请求作用域实例就继承 `SnowMiddlewareBase`(命名空间 `Snow.AspNetCore.Middleware`, 实现 `IMiddleware`, 容器按请求构造), 或用 `InvokeAsync` 参数注入。
- 中间件实现本身是原生写法, 包只负责把它包成 `UseXxx()`。宿主若在两个地方手写同一个 `UseXxx()`, 就会真的挂两遍 —— ASP.NET Core 自己也这样: 微软的 `UseAuthentication()` 连调两次不报错, 只是往 `app.Properties` 记一个 `__AuthenticationMiddlewareSet`。
- **测试兜底**: `Snow.AspNetCore.Tests` 里九条用例 —— 模块登记的端点动作被执行、服务注册三趟按序、三段初始化按序且收尾段幂等、收尾段先于第一段要炸、关停只跑一次, 外加四条静态资源合并。改管道逻辑前先跑 `dotnet test test/Snow.AspNetCore.Tests`。
- 与 ABP 对照: 机制完全相同 —— ABP 的 `AbpAspNetCoreModule` 不挂任何中间件, 整条管道写在宿主启动模块里(模板十几行); 这里也一样, 宿主写、框架不代劳。区别只是 Snow 把 `UseSnowCorrelationId()` 与 `UseSnowEndpoints()` 这类常用动作包成了扩展方法, 宿主少写两行。
- 与 Spring 对照: Spring 是 `Filter` 集合 + `@Order` 数字做全局排序, 库能自我装配, 代价是每个 filter 都要维护一个**全局**数字、还要知道别人的类名才能 `addFilterBefore`; 这里就是宿主自己的一串 `app.UseXxx()`, 顺序直接是行序, 没有数字。

## 后台任务: 两个端口, 各管一件事

- `IBackgroundJobManager` —— **入队**。四个 lambda 重载: `EnqueueAsync(() => ...)`、`EnqueueAsync(() => ...Async())`、`EnqueueAsync<TService>(s => s.Do())`、`EnqueueAsync<TService>(s => s.DoAsync())`。
- `IBackgroundJobScheduler` —— **调度**。周期注册 / 删除 / 立刻跑一次, 同样传表达式, 不写"类名 + 方法名"字符串。

队列用内置存储加 worker 就能跑; 周期调度需要装 `Snow.BackgroundJobs.Hangfire`。
没有实现时注入 `IBackgroundJobScheduler` 会在 DI 阶段直接报错, 而不是静默什么都不做。

### 1. 入队: 写方法调用, 不写作业类

```csharp
public sealed class OrderService(IBackgroundJobManager jobs)
{
    public async Task PaidAsync()
    {
        // 服务上的方法: 服务类型显式写出来, 执行时由容器解析
        await jobs.EnqueueAsync<IMailService>(mail => mail.SendAsync("a@b.c"));

        // 延迟执行
        await jobs.EnqueueAsync<IMailService>(mail => mail.SendAsync("a@b.c"), TimeSpan.FromMinutes(5));

        // 静态方法或无参 lambda
        await jobs.EnqueueAsync(() => Cache.WarmUp());
    }
}
```

入队时表达式被解析成"服务类型 + 方法 + 参数": 参数在这一刻求值并序列化进存储, 执行时按方法签名重载匹配、
反序列化、从容器取服务实例再调用。因此:

- 方法必须 public, 返回 `void` / `Task` / `ValueTask`; `async void` 直接拒绝。
- 参数里可以带 `CancellationToken`, 由执行方注入, 不入库。
- 扩展方法不支持 —— 包一层普通方法再入队。
- 表达式 body 必须是方法调用, 传 `() => Task.CompletedTask` 这类会立刻抛 `ArgumentException`。

执行语义只有三条:

- **成功** —— 删记录。
- **任务内部抛异常** —— 包装成 `BackgroundJobExecutionException`, 按 `RetryDelay * WaitFactor^(n-1)` 指数退避回写 `NextTryTime`, 达到 `MaxRetryCount` 丢弃。
- **任务解析不出来**(服务类型找不到、方法签名对不上、参数反序列化失败) —— 这类错误重试多少次都一样, 直接丢弃, 不浪费重试配额。

停机打断的 `OperationCanceledException` 不算失败, 记录保持原样等下次启动。

选项不读配置节(与 ABP 的 `AbpBackgroundJobOptions` 一样, 由宿主用代码给出), 执行开关与轮询参数都在同一个类里:

```csharp
Configure<BackgroundJobOptions>(options =>
{
    options.PollInterval = TimeSpan.FromSeconds(10);
    options.MaxRetryCount = 3;
});
```

装了 Hangfire 时那个模块会先把 `Enabled` 置 false; 宿主要调其它参数时别顺手把 `Enabled` 又写回 true, 否则内置 worker 会和 Hangfire 一起消费队列。

| 属性 | 默认 | 作用 |
| --- | --- | --- |
| `Enabled` | true | 是否启动内置 worker(装 Hangfire 时由那个模块自动置 false) |
| `PollInterval` | 5s | 轮询间隔 |
| `MaxJobFetchCount` | 100 | 单轮最多取多少条任务 |
| `RetryDelay` | 60s | 首次失败后等多久, 之后每次乘 `WaitFactor` |
| `WaitFactor` | 2.0 | 退避倍数 |
| `MaxRetryCount` | 5 | 最多尝试几次(含第一次) |
| `UseDistributedLock` / `DistributedLockName` | true / SnowBackgroundJobWorker | 多实例部署时避免重复消费 |
| `LockUnavailableDelay` | 60s | 没抢到锁时的退避时长, 免得空转 |

时间统一从 `IClock` 取(`Snow.Timing`), 换实现即可在测试里把时间拧到任意时刻。

### 2. 调度: 传表达式, 不传字符串

```csharp
public sealed class ReconService(IBackgroundJobScheduler scheduler)
{
    public void Start() => scheduler.ScheduleRecurring<ReconService>("daily-recon", "0 2 * * *", service => service.RunAsync());

    public void Stop() => scheduler.DeleteRecurring("daily-recon");

    public void RunNow() => scheduler.TriggerOnce<ReconService>(service => service.RunAsync());

    public Task RunAsync() => Task.CompletedTask;
}
```

表达式由调度框架直接消费, 中间没有"名字 → 类型 → 方法"的运行时查找, 所以不存在"类名写错"这类错误:
编译期就定死了调的是哪个方法。目标类要能被 DI 解析(Hangfire 用 `AspNetCoreJobActivator`)。

### 换成 Hangfire

```csharp
[DependsOn(typeof(SnowHangfireModule))]
public sealed class MyModule : SnowModule;

// 仪表盘中间件由宿主在管道里挂(见「中间件管道」), 需放在 UseAuthorization() 之后:
//   app.UseSnowHangfireDashboard();                                                            // 用选项里的 DashboardOptions(空则 Hangfire 默认)
//   app.UseSnowHangfireDashboard(o => o.Authorization = new MyDashboardAuthorizationFilter());  // 就地追加授权过滤器

// 持久化存储: 框架不绑定任何存储库, 在启动模块的 ConfigureServices 里用 Hangfire 自己的 AddHangfire 追加。
// 模块已经给过兼容级别/序列化器/序列化设置, 这里后加的存储排在后面, 持久化存储就盖掉默认值:
public override void ConfigureServices(ServiceConfigurationContext context)
{
    context.Services.AddHangfire(configuration => configuration
        .UseSqlServerStorage(context.Configuration.GetConnectionString("Hangfire")));
}
```

模块做的事: 给 Hangfire 套上兼容级别与序列化器这些样板, 关掉内置 worker(免得它空轮询), 按选项接上服务端, 并提供 `UseSnowHangfireDashboard()` 供宿主在管道里挂; 存储不归模块管, 宿主自己 `AddHangfire` 指定; 两个端口本身的替换不写代码 —— `HangfireBackgroundJobManager` / `HangfireBackgroundJobScheduler` 上标了 `ReplaceServices = true`, 约定注册会把默认实现顶掉(与 MailKit / FTP / Excel 那批同一套路)。
选项不读配置节(与 ABP 的 `AbpHangfireOptions` 一样由宿主给), 也不复刻 Hangfire 的字段: `EnableServer`(默认 true, 只入队不执行的实例设 false) / `ServerOptions`(Hangfire 的 `BackgroundJobServerOptions`, 线程数、轮询间隔、队列、服务名都在里面) / `DashboardOptions`(Hangfire 的 `DashboardOptions`, 标题与授权过滤器在里面) / `DashboardPath`(默认 `/hangfire`) / `TimeZoneId`。两项 `*Options` 留空就用 Hangfire 自己的默认值; 模块只在 `ServerOptions.Activator` 为空时补上容器里的激活器。存储完全由宿主决定(`AddHangfire(...UseXxxStorage)`), 模块不提供任何内置存储开关, 也不退回进程内存储 —— 没配存储时解析 `JobStorage` 会直接报 Hangfire 自己的错误。要改参数就在 `PreConfigureServices` 里 `Configure<SnowHangfireOptions>(options => ...)`。
仪表盘默认的授权过滤器是 Hangfire 的 `LocalRequestsOnlyAuthorizationFilter`, 只认本机回环地址; 部署在反向代理之后时它看到的是代理地址, 面板等于对外开放, 这种情况务必自己配 `DashboardOptions.Authorization`。

Hangfire 侧入队直接把 lambda 交给 `Job.FromExpression`, 仪表盘里显示的是真实的服务与方法
(`IMailService.SendAsync`), 不是一个统一的转发器。参数在入队时求值并由 Hangfire 序列化。

两种后端共用同一份契约与同一条调用约定, 所以同一段入队代码在进程内 worker 与 Hangfire 上行为一致。

## 认证: 一个模块, 一项必填

`Snow.AspNetCore.Authentication` 一个包, 包内按职责分两个目录 —— 只分文件位置, 不分包, 命名空间统一 `Snow.AspNetCore.Authentication`,
所以引一个 `Snow.AspNetCore.Authentication` 就够, 不存在多引/漏引的问题:

| 目录 | 装什么 |
| --- | --- |
| `Abstractions/` | 契约与模型: `ITokenService` / `ITokenSessionStore`、`TokenSession` / `TokenClientInfo`、`SnowAuthenticationOptions`、`TokenException` 三兄弟(都实现 `IHasHttpStatusCode`) |
| 根目录 | 实现与模块: `TokenService`、`TokenKeys`(密钥与校验参数, internal)、`DistributedCacheTokenSessionStore`、模块 `SnowAuthenticationModule` |

宿主什么都不用做: 令牌异常由全局 `UseSnowExceptionHandling()` 统一翻译成 401/403 与统一错误信封,
`SnowAuthenticationModule` 不注册任何 MVC 过滤器。
本包不依赖 `Snow.AspNetCore`: 与 ABP 的 `Volo.Abp.AspNetCore.Authentication.JwtBearer` 一样,
`AddHttpContextAccessor()` 由模块自己登记(登录来源 IP/UA 靠它), 宿主是不是 Web 由业务决定,
包本身只压 `Snow.Core` / `Snow.Security` / `Snow.Authorization` / `Snow.Timing`。

`SnowAuthenticationModule` 的方案名是 `SnowAuthentication`, 处理器是框架自带的
`JwtBearerHandler` —— 验签、拼 `WWW-Authenticate` 都由框架完成, 本包只补它没有的两件事:
用 HS256 签发令牌, 以及验签通过后再查一次服务端会话(白名单)。

模块不读配置节(与 ABP 的 `AddAbpJwtBearer` 一样由宿主决定来源), 启动模块里显式绑一节配置或直接赋值:

```csharp
// 启动模块的 ConfigureServices
Configure<SnowAuthenticationOptions>(context.Configuration.GetSection("Authentication"));
```

只有一项必填:

```json
{
  "Authentication": {
    "SecretKey": "openssl rand -base64 48 的输出"
  }
}
```

- `SecretKey` —— HS256 的签名密钥, **唯一必填项**, 至少 32 字节。用 `openssl rand -base64 48` 生成,
  别提交进仓库: 用环境变量、user-secrets 或密钥管理服务注入。它既是签名密钥也是验签密钥,
  每个校验方都得持有一份, 任一处泄露即可伪造任意用户令牌 —— 单实例或密钥不外流时用它最省事。
- `Issuer` —— 写进令牌的 `iss`, 留空按 `"Snow"`。本包自签自验, 它没有安全作用,
  只是让令牌符合 RFC 7519 的常规形态, 将来接入别的服务时方便区分来源。
- `Audience` —— 写进令牌的 `aud`, 留空按 `"Snow"`。同上, 自签自验时不影响安全。
- `Expiration` —— 会话有效期(滑动), 留空按 24 小时。

密钥不合规(为空或不足 32 字节)时应用起不来, 不会静默降级; 其余三项都能省。

`Expiration` 是 `TimeSpan`, 格式是 `[天.]时:分:秒` —— 24 小时要写 `"1.00:00:00"`,
写成 `"24:00:00"` 会被解析成 24 **天**(省略天数时第一段仍是天, 不是时)。

`Expiration` 是滑动窗口, **也是唯一的有效期**: 令牌里的 `exp` 只是签发时的快照, 框架侧的有效期校验被关掉了,
真正判过期的是服务端会话的到期时间。每次访问都把会话到期时间推到当前时间加一个窗口,
所以一直在用就一直在线, 永远不需要重新登录; 停用超过一个窗口才需要重新登录。改这个值对已签发的令牌同样生效。

令牌固定从请求头 `Authorization: Bearer <token>` 读取(RFC 6750)。不支持 Cookie 与查询参数:
两者都会把令牌写进访问日志、浏览器历史与 Referer, Cookie 来源还得再处理 CSRF, 收益抵不上风险。
请求头格式不符(比如 `Basic xxx`)时视为未携带令牌, 不会去别处找。

注入 `ITokenService` 做签发与校验。

### 1. 登录与登出

```csharp
// 账号、名称与附加声明(声明会写进令牌)
string token = await tokenService.LoginAsync(42, "alice", "爱丽丝", [
    new("permission", "Snow.Orders.Create"),
    new("role", "admin"),
]);

await tokenService.LogoutAsync(token);
```

登录端点自己写(本包不映射任何端点), 记得标 `[AllowAnonymous]`: 模块把全站默认设成要求登录,
否则登录请求自己就被 401 拦住了。

令牌自带声明, 校验时不需要查库还原用户。会话只存 `jti` → 账号与时间, 用来让令牌提前失效:
签名有效、会话还在且没过期, 两者同时成立才算登录。登出、踢人、封禁都是删会话, 令牌立刻失效。
会话的到期时间每次访问都往后走, 缓存键的 TTL 与它同步, 停用超过一个窗口自然回收。

### 2. 注解鉴权

用 ASP.NET Core 内置特性, 本包不提供自定义注解:

```csharp
[Authorize]                                          // 只要求登录
[Authorize(Roles = "admin,ops")]                     // 角色, 具备任一即通过
[Permission("Snow.Orders.Create")]                   // 权限, 走 Snow.Authorization 的权限定义与校验
```

角色声明直接来自令牌(`RoleClaimType` 已指向 `SnowClaimTypes.Role`)。
权限注解写 `[Permission("权限名")]`(单参数场景也可写 `[Authorize("权限名")]`), 由 `SnowAuthorizationPolicyProvider` 翻译成 "要求已登录 + 拥有该权限"。
权限必须先定义, 名字写错时拿不到策略, 请求抛 500 而不是 403。

全站默认要求登录, 匿名接口用 `[AllowAnonymous]` 放行。

### 3. 踢人、封禁与在线数

```csharp
await tokenService.KickoutAsync(42);                        // 删该账号全部会话, 令牌立即失效
await tokenService.GetOnlineCountAsync(42);                 // 在线端数
await tokenService.DisableAsync(42, TimeSpan.FromHours(1)); // 封禁: 踢人 + 之后登录抛 TokenDisabledException
await tokenService.EnableAsync(42);
```

同一账号允许同时登录多处。会话靠滑动续期维持: 一直在用就一直有效, 停用超过 `Expiration` 一个窗口自己到期。
被泄露的令牌不会自己过期 —— 改密码、改角色、禁用账号时记得调 `KickoutAsync`。

### 4. 存储

存储只有一个实现 `DistributedCacheTokenSessionStore`, 全部落在 `IDistributedCache` 里, 三类键:
`snow:jsonwebtoken:`(会话, 校验要读)、`snow:jsonwebtoken-account:`(账号 → 令牌索引)、
`snow:jsonwebtoken-disabled:`(封禁)。续期时会话键与账号索引一起续, 长期在线的账号不会出现索引先过期导致踢人漏设备。
账号索引的寿命另给 30 天下限, 不跟单条会话的到期时间走:
否则同账号先登录的长会话会被后登录的短会话把索引 TTL 压短, 索引先死会让踢人与在线数漏掉仍然有效的令牌。
模块**不注册** `IDistributedCache`, 只在解析会话存储时取容器里已有的实例:
宿主接 `Snow.Caching.StackExchangeRedis`(或任何 `IDistributedCache` 实现)就是分布式存储, 多实例共享, 踢人与封禁跨实例生效;
一个都没接才回退进程内实现, 单实例开箱可用, 启动时会打一条 warning 提示多实例下不生效。

所以换后端不用改代码, 也不用管模块装配顺序: 本模块不占用那个注册槽位, 宿主在它之前或之后登记缓存都生效。

注意: 所有实例必须共享同一个 `IDistributedCache`, 否则对方实例查不到会话, 令牌会被判为无效
(不是踢人不生效, 是全部登不上)。

当前登录态直接读 `HttpContext.User`(账号、名称、角色都在令牌声明里), 本包不再提供静态门面。
登录、登出、踢人、封禁请构造注入 `ITokenService`, 权限校验请用 `IPermissionChecker`。

### 5. 异常映射

三个异常都继承 `SnowException` 并实现 `IHasHttpStatusCode`, 由全局 `UseSnowExceptionHandling()` 统一翻译, 本包不再自带中间件或 MVC 过滤器:

| 异常 | 状态码 | 说明 |
| --- | --- | --- |
| `TokenNotLoginException` | 401 | 未登录 |
| `TokenNotRoleException` | 403 | 缺少角色 |
| `TokenDisabledException` | 403 | 账号已被封禁 |

响应体是统一的 `SnowRemoteServiceErrorResponse`, 与其它框架异常同一形状; 管道里挂了 `UseSnowExceptionHandling()`(见"宿主管道")即可, 无需额外步骤。
`JwtBearer` 在认证阶段抛出的令牌异常同样会被它兜住, 链路标识仍在最外层带回 `X-Correlation-Id`。


## 权限: 定义一次, 注解即用

`Snow.Authorization` 管"能干什么", 认证(谁是你)由 `Snow.AspNetCore.Authentication` 负责。权限本身随令牌以声明形式传递, 框架不维护授予表。

### 1. 定义权限

继承 `PermissionDefinitionProvider`, 只覆写 `Define`:

```csharp
public sealed class OrdersPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(PermissionDefinitionContext context)
    {
        PermissionGroupDefinition group = context.AddGroup("Orders", "订单管理");

        PermissionDefinition create = group.AddPermission("Snow.Orders.Create", "新建订单");
        create.AddChild("Snow.Orders.Create.Batch", "批量新建");

        // 停用的权限永远校验不通过
        group.AddPermission("Snow.Orders.Delete", "删除订单").Disable();
    }
}
```

**不需要任何手动登记**: 模块在 `ConfigureServices` 里扫一遍模块清单的全部程序集, 找到所有
`PermissionDefinitionProvider` 的子类自动登记(约定注册只按接口或自身类型登记, 不会登记抽象基类的实现,
所以这里补扫一遍)。只要你的模块程序集进了 `[DependsOn]` 链, 类型就在扫描范围内。定义只在第一次访问时构建一次,
之后走缓存。

### 2. 注解鉴权

```csharp
[Permission("Snow.Orders.Create")]                                  // 一个权限
[Permission("Snow.Orders.Create", "Snow.Orders.Update")]            // 多个, 满足其一即可
[Permission("Snow.Orders.Create", "Snow.Orders.Update",
                     Mode = PermissionMatchMode.All)]                        // 多个, 都要有
```

策略名由注解自己编出来(`perm-any:A,B` / `perm-all:A,B`), 再交给 `SnowAuthorizationPolicyProvider` 解析成
"要求已登录 + 权限要求", 不用为每个权限写 `AddPolicy`。策略对象构造一次后缓存。

**为什么不用 `[Authorize("A", "B")]`** —— 框架的 `AuthorizeAttribute` 只有无参与单参两个构造, 双参写不出来:

```text
error CS1729: "AuthorizeAttribute"不包含采用 2 个参数的构造函数
```

单参的 `[Authorize("A")]` 仍然可用(等价于 `[Permission("A")]`); 逗号字符串 `[Authorize("A,B")]` 也兼容,
含义是"满足其一", 但容易被误读成一个长权限名, 所以推荐用 `Permission`.

三种多个权限的写法, 语义对照:

| 写法 | 语义 | 由谁实现 |
| --- | --- | --- |
| `[Permission("A","B")]` | 满足其一 | `AnyPermissionRequirement` |
| `[Permission("A","B", Mode = PermissionMatchMode.All)]` | 都要有 | 每个权限一条 `PermissionRequirement` |
| `[Authorize("A")] [Authorize("B")]` | 都要有 | ASP.NET Core 合并多个特性, 与提供器无关 |

"满足其一"的处理器按书写顺序逐个问 `IPermissionChecker`, 命中即停; 全部不命中才 403。

前提是每个权限都已被 `PermissionDefinitionProvider` 定义: 多个名字里只要有一个没定义, 请求时拿不到策略,
授权中间件就抛 `InvalidOperationException`(500) —— 拼错的权限名不会因为旁边那个是对的而被放过。

### 3. 代码里手判

```csharp
public sealed class OrderAppService(IPermissionChecker permissionChecker)
{
    public async Task CreateAsync()
    {
        if (!await permissionChecker.IsGrantedAsync("Snow.Orders.Create"))
        {
            throw new UnauthorizedAccessException("没有新建订单的权限。");
        }
    }
}
```

没有批量重载: 要判多个权限就逐个 `await`。单次判定是纯内存的声明比对, 不发 IO。

### 4. 权限从哪来: 令牌里的声明

框架不保存"谁被授予了什么"。签发令牌时把该用户拥有的权限名写成 `permission` 声明(可多条):

```csharp
string token = await tokenService.LoginAsync(
    userId: 42,
    userName: "admin",
    name: "管理员",
    claims:
    [
        new(UserClaimTypes.Role, "admin"),
        new(ClaimsPermissionChecker.PermissionClaimType, "Snow.Orders.Create"),
        new(ClaimsPermissionChecker.PermissionClaimType, "Snow.Orders.Delete"),
    ]);
```

判定就是一次声明比对(`ClaimsPermissionChecker`): 身份里存在同名 `permission` 声明就算拥有; 带 `"*"`
(`ClaimsPermissionChecker.AllPermission`)等于拥有全部权限。没有存储调用, 没有跨实例同步, 重启不丢 ——
权限跟着令牌走。

代价写在明处: **权限变更要等令牌重新签发**。要求改动立刻生效, 就把令牌有效期调短配合刷新, 或者换掉校验器按请求查库(见下)。

角色怎么映射成权限由你决定: 签发时按角色展开成一条条 `permission` 声明, 或者让校验器自己去查角色表, 框架不预设。

### 判定规则

| 条件 | 结果 |
| --- | --- |
| 权限未定义 / 被 `Disable()` | 不通过(先于声明比对, 打错字不会因为同名声明而放行) |
| 身份里没有同名 `permission` 声明 | 不通过 |
| 身份里有 `permission = 权限名` 或 `permission = *` | 通过 |

换校验器就是顶掉 `IPermissionChecker` 的登记, 一行 `Replace`:

```csharp
services.Replace(ServiceDescriptor.Transient<IPermissionChecker, DbPermissionChecker>());   // 按请求查库, 改动立刻生效
```

框架不额外提供包装方法: 生命周期必须与默认实现一致(Transient), 写成 Singleton 会把 `ICurrentPrincipalAccessor` 变成 captive dependency。

**为什么框架不自己存授予表** —— 一张"谁被授予了什么"的表放进程里, 重启清空、多实例各存一份; 放数据库又等于替宿主
定了数据模型。Sa-Token 不存(每次回调宿主接口去查), ABP 存库(EF Core 实体), 我们跟随前者: 权限跟着令牌走,
改动靠重新签发。需要"改了立刻生效"的宿主, 用自定义校验器那一行换掉即可。

## HTTP 客户端

`Snow.Http.Client` 把 ASP.NET Core 的 `IHttpClientFactory` 接进来, 对外只有两种用法: **声明式接口**(对应 Spring 的 `@HttpExchange` 接口, 推荐) 和 **低层访问器**(对应注入一个配好的 `RestClient`, 用于流、非 JSON 与一次性调用)。

```csharp
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
除此之外的能力都不在本包里: 应用层、认证、缓存、分布式锁、后台作业、事件总线、消息、邮件、FTP、HTTP 客户端、Excel、工作流、EntityFrameworkCore、数据过滤(`Snow.Data`)、AOP(`Snow.Aop.Castle`) 以及它们的第三方实现, 由宿主按需引用。ABP 也是这个做法: `Volo.Abp.AspNetCore` 只依赖 9 个基础模块, Caching / EventBus / BackgroundJobs 全部留给宿主声明。
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
        app.UseSnowTokenExceptionHandling();   // 3  令牌异常兜底, 必须在 UseAuthentication 之外
        app.UseRouting();                      // 4  路由
        app.UseAuthentication();               // 5  认证
        app.UseSnowMultiTenancy();             // 6  多租户, 认证之后才读得到租户声明
        app.UseSnowRateLimiting();             // 7  限流, 路由之后才读得到端点元数据
        app.UseAuthorization();                // 8  授权
        app.UseSnowHangfireDashboard();        // 9  终端分支, 必须晚于授权
        app.UseSnowWorkflows();                // 10 终端分支
        app.UseSnowEndpoints();                // 11 端点, 永远最后
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

- **`UseSnowTokenExceptionHandling()` 在 `UseAuthentication()` 之前**: 它在最外层才接得住 `JwtBearer` 抛的令牌异常, 否则令牌过期返回 500 而不是 401。
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

`Snow.Authentication` 一个包, 包内按职责分三个目录 —— 只分文件位置, 不分包, 命名空间统一 `Snow.Authentication`,
所以引一个 `Snow.Authentication` 就够, 不存在多引/漏引的问题:

| 目录 | 装什么 |
| --- | --- |
| `Abstractions/` | 契约与模型: `ITokenService` / `ITokenSessionStore` / `ITokenKeyProvider`、`TokenKeySet`(密钥与签发凭据)、`TokenSession` / `TokenClientInfo` / `TokenInfo`、`TokenResolver`(从 `Authorization: Bearer` 取令牌)、`SnowAuthenticationOptions`、`TokenException` 三兄弟 |
| `AspNetCore/` | Web 适配: 把登录态异常翻译成 401/403 的 `TokenExceptionHandling` / `TokenExceptionFilter` / `TokenExceptionMiddleware` 与宿主调的 `UseSnowTokenExceptionHandling()` |
| 根目录 | 实现与模块: `TokenService`、`OptionsTokenKeyProvider`、`DistributedCacheTokenSessionStore`、`TokenUtil` 门面、注解鉴权(`[TokenCheckLogin]` / `[TokenCheckRole]`)、模块 `SnowAuthenticationModule` |

MVC 宿主什么都不用做: `SnowAuthenticationModule` 自己用 `options.Filters.AddService(typeof(TokenExceptionFilter))` 登记过滤器;
最小 API 宿主在管道里挂 `UseSnowTokenExceptionHandling()`。

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

注入 `ITokenService` 做签发与校验, 它返回的令牌信息类型是 `TokenInfo`。

### 1. 登录与登出

```csharp
// 只给用户Id
string token = await tokenService.LoginAsync(42);

// 带账号、名称与附加声明(声明会写进令牌)
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

```csharp
[TokenCheckLogin]                                  // 只要求登录
[Permission("Snow.Orders.Create")]        // 权限, 走 Snow.Authorization 的权限定义与校验
[TokenCheckRole("admin", "ops")]                   // 角色, 具备任一即通过
```

登录与角色注解只是把策略名写成 `snowauth:login` / `snowauth:role:...`, 由 `TokenPolicyProvider` 动态翻译成要求。
权限注解写 `[Permission("权限名")]`(也可以给一个就够的场景写 `[Authorize("权限名")]`): 策略名就是权限名,
由 `SnowAuthorizationPolicyProvider` 动态翻译成 "要求已登录 + 拥有该权限", 与登录/角色注解共用同一份策略缓存。
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

### 4. 存储与门面

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

静态门面 `TokenUtil` 只保留"拿当前请求"这一件注入做不了的事, 全部是同步的纯内存读:
`IsLogin` / `GetLoginIdOrNull` / `GetLoginId` / `CheckLogin` / `HasRole` / `CheckRole` / `GetTokenValue`。
登录、登出、踢人、封禁请构造注入 `ITokenService`, 权限校验请用 `IPermissionChecker`。

### 5. 异常映射

`TokenUtil.GetLoginId()` / `CheckLogin()` / `CheckRole()` 抛的异常由模块统一翻译成 HTTP 响应, 不用自己写过滤器:

| 异常 | 状态码 | 说明 |
| --- | --- | --- |
| `TokenNotLoginException` | 401 | 未登录 |
| `TokenNotRoleException` | 403 | 缺少角色 |
| `TokenDisabledException` | 403 | 账号已被封禁 |

响应体是 `ProblemDetails`(`application/problem+json`)。MVC 宿主由 `SnowAuthenticationModule` 自动注册的 `TokenExceptionFilter` 接管(它用 `options.Filters.AddService(typeof(TokenExceptionFilter))` 登记, 生成的是 `ServiceFilterAttribute`, `[DisableSnowFeatures(DisableMvcFilters = true)]` 才摘得掉);
最小 API 由宿主在管道里 `app.UseSnowTokenExceptionHandling()` 接管(须排在 `UseAuthentication()` 之前) —— 这样 `JwtBearer` 在认证阶段抛的令牌异常也能被它兜住。链路标识放管道最外层, 异常被捕获时链路作用域还活着, 错误响应能带回 `X-Correlation-Id`。
只拦上面三类, 其它异常原样抛出, 不影响宿主自己的错误处理。


## 权限: 定义一次, 注解即用

`Snow.Authorization` 管"能干什么", 认证(谁是你)由 `Snow.Authentication` 负责。权限本身随令牌以声明形式传递, 框架不维护授予表。

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

换校验器一行:

```csharp
services.AddSnowPermissionChecker<DbPermissionChecker>();   // 按请求查库, 改动立刻生效
```

**为什么框架不自己存授予表** —— 一张"谁被授予了什么"的表放进程里, 重启清空、多实例各存一份; 放数据库又等于替宿主
定了数据模型。Sa-Token 不存(每次回调宿主接口去查), ABP 存库(EF Core 实体), 我们跟随前者: 权限跟着令牌走,
改动靠重新签发。需要"改了立刻生效"的宿主, 用自定义校验器那一行换掉即可。

## HTTP 客户端

`Snow.Http.Client` 把 ASP.NET Core 的 `IHttpClientFactory` 接进来, 对外只有两种用法: **声明式接口**(对应 Spring 的 `@HttpExchange` 接口, 推荐) 和 **低层访问器**(对应注入一个配好的 `RestClient`, 用于流、非 JSON 与一次性调用)。

```csharp
[DependsOn(typeof(SnowHttpClientModule))]
public class MyModule : SnowModule { }
```

选项不读配置节(与 ABP 的 `AbpHttpClientOptions` 一样由宿主给), 在启动模块的 `PreConfigureServices` 里绑一节或直接赋值:

```csharp
Configure<SnowHttpClientOptions>(context.Configuration.GetSection("HttpClient"));
// 或: Configure<SnowHttpClientOptions>(options => { options.BaseAddress = "https://api.example.com"; options.UserAgent = "Snow/1.0"; options.DefaultHeaders["X-Tenant"] = "acme"; });
```

### 声明式接口(推荐, 对应 Spring 的 @HttpExchange)

把 HTTP 调用写成接口加注解, 运行时由 `DispatchProxy` 生成实现, 不需要实现类、不需要逐个登记地址:

```csharp
[HttpExchange("/api/BomOrder")]
public interface IHkoTrackingClient
{
    [PostExchange("/BulkAddBomOrder")]
    Task<ApiResponse<BomOrderResult>> BulkAddBomOrderAsync([Body] BomOrderBatchRequest request, CancellationToken cancellationToken = default);

    [GetExchange("/Get/{id}")]
    Task<ApiResponse<string>> GetAsync([Route] string id, [Query] string? keyword, [Header("X-Trace")] string? trace = null, CancellationToken cancellationToken = default);

    [DeleteExchange("/Delete/{id}")]
    Task DeleteAsync([Route] string id, CancellationToken cancellationToken = default);
}
```

```csharp
context.Services.AddSnowHttpServiceClient<IHkoTrackingClient>();
// 或一次登记多个: context.Services.AddSnowHttpServiceClients(typeof(IHkoTrackingClient), typeof(IOrderClient));
```

- 类型级 `[HttpExchange]` 写基路径; 方法级 `[GetExchange]` / `[PostExchange]` / `[PutExchange]` / `[DeleteExchange]` / `[PatchExchange]` 写动词与子路径。也可以用方法级 `[HttpExchange("/x", Method = "GET")]` 指定任意动词, 与 Spring 一致。
- 路径里的 `{name}` 由同名 `[Route]` 参数填充; 缺动词注解、占位没填、复杂参数没标注都直接抛 `SnowHttpServiceException`, 不会静默发请求。
- 参数: `[Body]` 是请求体(只允许一个; 只写一个复杂类型参数时也可省略, 自动当请求体), `[Query]` 拼查询串, `[Header]` 加请求头, `[Route]` 填占位; 简单类型(基元/枚举/字符串/日期/时间/Guid/Uri 等)不写 `[Query]` 也默认当查询参数。
- 返回类型支持 `Task` / `Task<T>` / `ValueTask` / `ValueTask<T>`; 非 2xx 抛 `SnowHttpServiceException`(`Method` / `Url` / `StatusCode` / `ResponseBody`), 反序列化用 `JsonSerializerDefaults.Web`(camelCase)。
- 契约里的泛型响应要写全实参(如 `ApiResponse<BomOrderResult>`), 不要写裸 `ApiResponse` —— 否则反序列化结果会丢成字典。

### 低层访问器 ISnowHttpClient<T>(对应注入 RestClient)

流、非 JSON、需要自己看状态码的分支, 直接注入访问器:

```csharp
public class FileService(ISnowHttpClient<IFileClient> fileClient)
{
    public async Task<Stream> OpenAsync(CancellationToken ct)
    {
        using HttpResponseMessage response = await fileClient.HttpClient.GetAsync("/files/1", HttpCompletionOption.ResponseHeadersRead, ct);
        return await response.Content.ReadAsStreamAsync(ct);
    }
}
```

也可以不声明契约, 用工厂取默认客户端(已带基础地址、超时、UserAgent 与默认请求头):

```csharp
HttpClient client = httpClientFactory.CreateSnowClient();
string text = await client.GetStringAsync("/orders");
```

名字由契约类型推导: 去掉前导 `I` 与尾部 `Client`(`IHkoTrackingClient` 得到 `HkoTracking`, `IMDDB65127Client` 得到 `MDDB65127`)。具名客户端的专属选项写在 `SnowHttpClientOptions.Clients` 里, 整份替换默认值而不是叠加; 没配的名字回退顶层默认值:

```csharp
Configure<SnowHttpClientOptions>(options =>
{
    options.Timeout = TimeSpan.FromSeconds(5);
    options.UserAgent = "Snow/1.0";
    options.Clients["HkoTracking"] = new SnowHttpClientOptions { BaseAddress = "https://hko.example.com" };
    options.Clients["MDDB65127"] = new SnowHttpClientOptions { BaseAddress = "https://mddb.example.com", Timeout = TimeSpan.FromSeconds(30) };
});
```

声明式接口与低层访问器共用这套命名与配置: 同一个实现类挂多个契约时, 每个契约按自己的名字取地址, 所以 `IMDDB65127Client` 与 `IMDDB65102Client` 可以有不同基础地址。

访问器是瞬态的, 每次注入一个新的 `HttpClient`; 不方便构造注入时用 `serviceProvider.CreateSnowHttpClient<IHkoTrackingClient>()`, 用完释放。提交给工厂的 `HttpClient` 由框架统一管理处理器生命周期(连接复用、DNS 变更自动更新), 不要在业务里 `new HttpClient()` 长期持有。

## Excel 导入导出

`Snow.Excel` 只有契约与静默兜底实现, 真正读写 xlsx 需再引 `Snow.Excel.ClosedXML`:

```csharp
[DependsOn(typeof(SnowExcelClosedXmlModule))]
public class MyModule : SnowModule;

// 注入 IExcelService 即可
byte[] bytes = excel.Export(orders);            // 按属性名做表头
List<OrderRow> rows = excel.Import<OrderRow>(bytes);  // 按表头文字导回
byte[] table = excel.Export(dataTable);         // DataTable 也行
```

列映射全在注解上, 不写一行配置:

```csharp
public sealed class OrderRow
{
    [ExcelIgnore] public Guid Id { get; set; }                                  // 不导出也不导入
    [ExcelColumn("订单号", Order = 1)] public string No { get; set; } = "";
    [ExcelColumn("金额", Order = 2, Format = "#,##0.00")] public decimal Amount { get; set; }
    [ExcelColumn("创建时间", Order = 3)] public DateTime Created { get; set; }
    [ExcelColumn("状态", Order = 4)] public OrderStatus Status { get; set; }    // 枚举按名字读回
}
```

- 导出: 表头加粗、冻结首行、自动筛选、按内容调列宽。行数上万时把 `AutoFitColumns` 关掉 —— 自动列宽要逐格量文本, 是导出里最慢的一步。
- 导入: 表头文字与 `[ExcelColumn]` 或属性名按忽略大小写匹配; 也可 `HasHeaderRow = false` 按属性声明顺序对列; 表头一个都对不上会抛 `InvalidOperationException` 并把实际表头列出来。
- 数值在 Excel 里是 double: 导回 `decimal` 时取最短往返表示(9.9 就是 9.9, 不带 `9.9000000000000003` 的二进制尾巴)。
- 需要合并单元格、条件格式、图表、多工作表时注入 `IClosedXmlExcelService`, 用 `CreateWorkbook()` / `OpenWorkbook(stream|bytes)` 拿原生 `XLWorkbook`(记得释放)。

不引实现包也能启动, 但调用导出或导入会立刻抛异常并提示装 `Snow.Excel.ClosedXML` —— 不返回空字节或空列表假装成功。

## 工作流(Elsa)

`Snow.Workflows` 只有契约与静默兜底实现, 真正跑工作流需再引 `Snow.Workflows.Elsa`(内置 Elsa 3 引擎、HTTP 端点、管理 API 与 SQLite 持久化):

选项不读配置节(与 ABP 一致), 由启动模块在 `PreConfigureServices` 里给出。代码级设置(工作流类型、Elsa 特性回调)走同一个入口 —— 没有额外的 `builder.Services` 扩展方法:

```csharp
[DependsOn(typeof(SnowElsaModule))]
public class MyModule : SnowModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        // 全部模块的 PreConfigureServices 跑完才开始任何模块的 ConfigureServices, 所以 Elsa 模块读得到这些值
        Configure<SnowElsaOptions>(options =>
        {
            options.ConnectionString = "Data Source=elsa.db";          // 非空即 SQLite 持久化, 留空则纯内存
            options.WorkflowTypes.Add(typeof(OrderApprovalWorkflow));   // 代码定义的工作流
            options.EnableHttp = true;
            options.HttpBasePath = "/workflows";
            options.EnableApi = true;

            // 一把梭: 换库 / 挂身份 / 改 BaseUrl 都在这层, 它在模块内置配置之后执行
            options.ConfigureElsa = elsa =>
            {
                // 身份: 管理员账号与令牌都用代码给; UseDevelopmentAdminUserProvider() 是 admin/password 的简写, 只适合开发
                elsa.UseIdentity(identity =>
                {
                    identity.UseAdminUserProvider(admin =>
                    {
                        admin.UserName = "admin";
                        admin.Password = "ChangeMe!123";
                        admin.Roles = ["admin"];
                    });
                    identity.TokenOptions = token => token.AccessTokenLifetime = TimeSpan.FromHours(8);
                });
            };
        });
    }
}
```

启动两种姿势:

```csharp
// 按定义标识(定义来自管理 API / 界面录入并已发布)
SnowWorkflowResult r1 = await runner.StartAsync("order-approval", new Dictionary<string, object?> { ["orderId"] = 42 });

// 按代码类型
SnowWorkflowResult r2 = await runner.StartAsync<OrderApprovalWorkflow>(input);

// Elsa 的 HTTP 处理由宿主在管道里挂: 在 UseAuthorization() 之后调 app.UseSnowWorkflows()(见「中间件管道」)。
```

- 结果 `SnowWorkflowResult` 只透出字符串状态(`Status` / `SubStatus`)与 `Incidents`, 不把 Elsa 的枚举泄漏到业务层; `Succeeded` 表示状态与子状态都是 `Finished`。
- 定义找不到、类型不可执行、引擎未安装都抛 `SnowWorkflowException`。
- 运行工作流所需的最小特性集总是挂上, 没有开关: 表达式求值、JavaScript 引擎、内存缓存, 以及管理与运行时的缓存。想省 JavaScript 依赖或换缓存实现, 在 `options.ConfigureElsa` 里补。
- 持久化只要 `ConnectionString` 非空就启用, 表结构在启动阶段自动迁移(`RunMigrations = true`); 交给 DBA 手工迁移就在 `ConfigureElsa` 里把 `ef.RunMigrations` 设成 `false`。
- 身份这类带取舍的特性不内置: 需要用户上下文时自己挂 —— `options.ConfigureElsa = elsa => elsa.UseIdentity(identity => { identity.UseAdminUserProvider(admin => { admin.UserName = "admin"; admin.Password = ""; }); identity.TokenOptions = token => token.AccessTokenLifetime = TimeSpan.FromHours(8); });`。管理员账号来自 `UseAdminUserProvider`, 也可以换成 `UseConfigurationBasedUserProvider` / `UseConfigurationBasedRoleProvider` / `UseConfigurationBasedApplicationProvider`(给固定清单), 或 `UseStoreBased*Provider`(走内存存储, 默认); `UseDevelopmentAdminUserProvider` 等价 admin/password, 只适合开发环境。
- 换 SQL Server / PostgreSQL: 自己加对应的 `Elsa.Persistence.EFCore.*` 包, 在 `ConfigureElsa` 里 `elsa.UseWorkflowManagement(m => m.UseEntityFrameworkCore(ef => ef.UseSqlServer(cs)))`(运行时同法)。模块的 SQLite 配置先执行, 这里后写的提供程序就是最终生效的那个。
- 分布式锁的默认提供者是**文件锁**, Elsa 把根目录写死在内容根下的 `App_Data/locks`, 每次启动自动重建; 它只协调共享同一本地文件系统的进程, 多容器/多机无效。想换位置就在 `ConfigureElsa` 里 `elsa.UseWorkflowRuntime(runtime => runtime.DistributedLockProvider = _ => new FileDistributedSynchronizationProvider(new DirectoryInfo(path)))`; 想换 Redis/PostgreSQL 等外部锁就赋对应实现。`DistributedLockingOptions` 只有 `LockAcquisitionTimeout` 与 `AllowLocalLockProviderInDistributedRuntime` 两个成员, **没有改目录的选项**。
- 不引实现包也能启动, 但调用 `ISnowWorkflowRunner` 会立刻抛 `SnowWorkflowException` 并提示装 `Snow.Workflows.Elsa`。
- 不想挂 HTTP 端点就把 `SnowElsaOptions.EnableHttp` 设为 `false`(管理 API 仍在); 想挂就按「中间件管道」在 `UseAuthorization()` 之后调 `app.UseSnowWorkflows()`。

## MVC 与自动 API

`SnowAspNetCoreMvcModule` 把应用服务按约定直接暴露成 Controller, 服务端不用写任何 Controller 类(对应 ABP 的 `AbpAspNetCoreMvcModule` + 自动 API)。整个机制三件事: 应用服务登记成 Controller(特性提供器)、路由与动词按约定算(应用模型约定)、结果与异常统一成信封(全局过滤器)。

```csharp
[DependsOn(typeof(SnowAspNetCoreMvcModule))]
public sealed class MyHttpApiHostModule : SnowModule { }
```

```csharp
// 类名以 ApplicationService / AppService / Service 结尾、且在消费方程序集里(框架自身的 Snow.* 程序集不参与),
// 就会被自动暴露成 API: 不需要 [ApiController], 也不需要继承任何基类或标记接口。
public class BookAppService
{
    public Task<BookDto> GetAsync(Guid id) => ...;              // GET api/app/book/{id}
    public Task<List<BookDto>> GetListAsync() => ...;           // GET api/app/book
    public Task<BookDto> CreateAsync(CreateBookDto input) => ...; // POST api/app/book
}
```

### 路由与动词约定

| 环节 | 规则 |
| --- | --- |
| 控制器名 | 类名去掉后缀 `ApplicationService` / `AppService` / `Service` 后转 kebab-case(`BookAppService` → `book`) |
| 控制器路径 | `{apiPrefix}`(默认 `api`) + `/` + `RootPath`(默认 `app`) + `/` + 控制器名, 即 `api/app/book` |
| GET 列表 | `GetList` / `GetAll` / `GetPaged` / `List` → `GET api/app/book` |
| GET 单条 | `Get` / `Find` / `Load`, 且有唯一的简单类型参数 `id` → `GET api/app/book/{id}`; 否则 → `GET api/app/book/{方法名 kebab}` |
| 新增 | `Create` / `Add` / `Insert` / `Post` → `POST api/app/book` |
| 修改 | `Update` / `Edit` / `Modify` / `Put` → `PUT`; `Patch` → `PATCH` |
| 删除 | `Delete` / `Remove` / `RemoveBy` → `DELETE` |
| 其余 | `POST api/app/book/{方法名 kebab}` |

参数绑定: 名为 `id` 的简单类型走路由; 复杂类型走请求体; 其余简单类型走查询串; `CancellationToken` 忽略。简单类型含 primitive、enum、string、decimal、Guid、各种日期时间类型、TimeSpan、Uri。

### 错误响应信封

控制器正常返回值保持原样, 不再包一层成功信封(ABP vNext 的 `Volo.Abp.AspNetCore.Mvc` 也没有成功包装过滤器)。异常与模型校验失败时统一返回 `SnowRemoteServiceErrorResponse`:

```json
{ "error": { "code": "ValidationError", "message": "请求参数校验失败",
             "details": null, "validationErrors": { "title": ["不能为空"] } } }
```

异常按类型翻译:

| 异常 | 状态码 | 错误码 |
| --- | --- | --- |
| `UserFriendlyException` | 400 | 异常自带的 `Code`, 未给则 `BusinessError` |
| `BusinessException`(以及任何实现了 `IBusinessException` 的异常) | 400 | 异常自带的 `Code`, 未给则 `BusinessError` |
| `UnauthorizedAccessException` | 403 | `Forbidden` |
| `KeyNotFoundException` | 404 | `NotFound` |
| `NotImplementedException` | 501 | `NotImplemented` |
| `ArgumentException` / `InvalidOperationException` | 400 | `InvalidArgument` |
| 其余 | 500 | `InternalError`(消息默认"服务器内部错误", 细节由 `SnowExceptionHandlingOptions.SendExceptionsDetailsToClients` 控制) |

模型校验失败自动回 400 与字段错误列表(`ValidationError`), 等价于 `[ApiController]` 的默认行为。

### 控制器级开关: `[DisableSnowFeatures]`

控制器上加一行 `[DisableSnowFeatures]`, 框架中间件、MVC 过滤器、AOP 拦截器三层一起跳过它 —— 三个开关默认全开(对齐 ABP 的 `DisableAbpFeaturesAttribute`), 只想关一层就显式把另一个写成 `false`:

```csharp
[DisableSnowFeatures]                            // 三层全跳过
public class LegacyController : SnowControllerBase { }

[DisableSnowFeatures(DisableMvcFilters = false)]  // 只跳中间件与拦截器, 信封与校验照常
public class HalfController : SnowControllerBase { }
```

| 开关 | 关掉什么 | 生效点 |
| --- | --- | --- |
| `DisableMiddleware` | 继承 `SnowMiddlewareBase`(`Snow.AspNetCore.Middleware`)的框架中间件(链路标识、日志上下文等) | `SnowMiddlewareBase.ShouldSkipAsync` |
| `DisableMvcFilters` | 自动校验、模型绑定校验、异常翻译、登录态异常 | 动作描述符定型时由 `SnowMvcActionDescriptorProvider` 把框架过滤器整个摘掉, 请求期零判断 |
| `DisableInterceptors` | AOP 拦截器(缓存等注解) | 约定注册阶段不建代理 |

- 特性在 `Snow.Core`(命名空间 `Snow`), 与 ABP 放在 `Volo.Abp.Core` 同位; 标注控制器的项目不必为了一个特性引用 `Snow.Security`。
- 判定只认控制器端点: 最小 API、静态文件、健康检查没有 `ControllerActionDescriptor`, 不受开关影响。判定函数收的是 `Type?` 而不是 `ActionDescriptor`, 所以 `Snow.Core` 不必认识 MVC 类型。
- 摘除机制照抄 ABP 的 `AbpMvcActionDescriptorProvider`: 框架过滤器一律用 `options.Filters.AddService(typeof(X))` 登记, 生成的是 `ServiceFilterAttribute`; 描述符定型后 `SnowMvcActionDescriptorProvider`(Order = -1000 + 10, 与 ABP 的 `AbpMvcActionDescriptorProvider` 同值 —— 排在 MVC 自带描述符提供器的 -1000 之后, 正序 Executing 时后跑、倒序 Executed 时先动手。命名空间 `Snow.AspNetCore.Mvc`, 与包内其余文件一致; ABP 那边写的是 `Volo.Abp.AspNetCore.Mvc.ApplicationModels`, 那个后缀抄自 ASP.NET Core 的 `Microsoft.AspNetCore.Mvc.ApplicationModels.ControllerActionDescriptorProvider`, 表达的是职责而非目录, 所以 Snow 不跟)只摘实现了 `ISnowFilter`(`Snow.AspNetCore.Filters`)的那些。用 `Filters.Add<X>()` 登记会生成 `TypeFilterAttribute`, 摘不掉, 开关对它无效。
- `DisableInterceptors` 看的是**实现类型**: 标在应用服务类上, 该服务不做代理。自动 API 的 Controller 由应用服务生成, 所以标在服务类上就等于标在 Controller 上; 标在生成出来的 Controller 类上不生效。
- 拦截器的形态与 ASP.NET Core 中间件一致: `abstract Task InvokeAsync(AspectContext context, AspectDelegate next, CancellationToken cancellationToken = default)`, 调 `await next(context, cancellationToken)` 才走到目标方法(不调即短路, 此时给 `context.ReturnValue` 赋值)。`AspectContext` 是抽象类(形状固定, 由代理工厂决定怎么造), 整条链共享一个实例(实参与返回值的修改对上下层都可见); Snow.Aop.Castle 里的 `DefaultAspectContext` 是包住 Castle 调用信息的默认实现, `next` 由参数传入而不是字段, 所以拦截链能按方法组装一次并缓存, 也不需要任何 `internal` 特权。`cancellationToken` 由适配器取被拦截方法签名里的 `CancellationToken` 参数(没有就是 `None`)交给整条链。
- 建代理这一步在 `Snow.Core` 的约定注册里, 但真正建代理的代码在 `Snow.Aop.Castle`: 约定扫描时读到实现类型上的 `SnowInterceptorAttribute`(类级 + 方法级, 并尊重 `DisableInterceptors`), 该类型的接口服务类型就换成工厂 lambda, 工厂从容器取 `IAopProxyFactory`; `CastleAopProxyFactory` 自己标着 `[SingletonService]`, 由约定注册扫进容器, 所以宿主只要让启动模块依赖 `SnowAopModule` 就有工厂, 取不到则抛「请让宿主模块依赖 `SnowAopModule`(`Snow.Aop.Castle` 包)以启用 AOP」。只打了拦截器注解、没标服务注解的类型按瞬态登记(做到「一个注解即可用」), 开放泛型直接报错 —— 代理需要具体类型。内核只认接口 `IAopProxyFactory`(定义在 `Snow.Core` 的 `Snow.Aop` 命名空间, 与 ABP 把 `IAbpInterceptor` / `ProxyHelper` 放在 `Volo.Abp.Core` 同理), 换引擎就换它的实现。
- 类代理(Castle 的 class proxy): 类型既暴露接口又暴露自身、还标了拦截器时, 实现类型自身那条描述符建成类代理, 接口描述符转发到它 —— 解析接口与解析具体类型拿到同一个被代理对象, 没有「按具体类型注入就绕过拦截」的缺口。三个前提: 具体类(`sealed` / `abstract` 建不出来)、有可重写的虚方法(Castle 只拦 `virtual`)、有能从容器解析出参数的公开构造器(取不到就用参数的默认值, 连默认值都没有才抛 `ArgumentException`)。标了拦截器的方法却不是 `virtual`(例如接口方法的非虚实现)时, 建代理当场抛 `ArgumentException` 指出是哪个方法, 不静默放行。类级拦截器覆盖全类, 但 Castle 的类代理只拦 `virtual`, 会被静默跳过的成员在启动时一次性列成告警(每个类型只报一次); `async void` 方法的完成也拦不住(拦截链在方法真正结束之前就收尾), 同样告警一次, 建议改成 `async Task`。既暴露自身、又标了拦截器、却做不出类代理的类型(密封类, 或一个虚方法都没有), 启动期直接抛 `InvalidOperationException`: 否则那条服务没人登记, 直到第一次按具体类型解析才报「没有以 X 注册的服务」; 只按接口解析的类型不受影响, 接口代理照常。拦截器注解标在接口方法上、或标在显式接口实现(`string IFoo.Bar()`)上都生效 —— 约定扫描会看实现类型的非公开方法与它实现的接口, 适配器再顺着接口映射把声明处与实现处的注解一起读出来(声明处在外层)。拦截器清单与拦截链都按「实现类型 + 方法」静态缓存, 瞬态服务反复解析不重复反射。
- 代理对象的运行时类型是 Castle 生成的子类(命名空间 `Castle.Proxies`), 不是实现类型本身: `service.GetType() == typeof(UserService)`、按具体类型 `as` 转换、拿对象当字典键都会因此失效。要取回原始实例用 `SnowProxyHelper.UnProxy(obj)`: 接口代理沿 `__target` 字段找回实现类型的实例, 类代理本身就是实现类型的子类、只有一份对象, 原样返回(此时 `as` / `is` 判断不受影响, 只有 `GetType()` 的精确比较会看到代理类型), 普通对象原样返回。

### 接线与配置

```csharp
WebApplication app = builder.Build();
await app.InitializeApplicationAsync();   // 三段模块初始化 + 填对象访问器
app.Run();
```

选项默认值写在 `SnowAspNetCoreMvcOptions` 里(`AutoModelValidation` 默认 `true`, `ExposeIntegrationServices` 默认 `false`, 与 ABP 的 `AbpAspNetCoreMvcOptions` 一样由选项类给默认值, 模块不读配置节), 要改就在 `AddApplication` 之前用 `builder.Services.PostConfigure<SnowAspNetCoreMvcOptions>(options => ...)`(必须在 `AddApplication` 之前, 那时自动 API 约定还没建)。

## OpenAPI 文档与调试界面

`SnowOpenApiModule` 用官方 `Microsoft.AspNetCore.OpenApi` 生成文档, 用 Scalar 提供调试界面(对位 ABP 的 `Volo.Abp.Swashbuckle`)。
自动带上全部自动 API 的路径, 可选加 Bearer 认证方案, 并自动把 XML 注释贴成接口说明。依赖 `SnowAspNetCoreMvcModule`。

```csharp
[DependsOn(typeof(SnowOpenApiModule))]
public sealed class MyHttpApiHostModule : SnowModule { }
```

```csharp
app.Run();   // 文档端点已由 SnowOpenApiModule 登记, 默认 /openapi/v1.json 与 /openapi 界面
```

单独使用本包(宿主没有依赖 `SnowAspNetCoreModule`)时才需要手写 `app.UseSnowOpenApi();`。

选项不读配置节(与 ABP 一致): 在启动模块的 `PreConfigureServices` 里 `Configure<SnowOpenApiOptions>(...)`, 或用 `builder.Services.AddSnowOpenApi(options => ...)`(必须在 `AddApplication` 之前, 那时模块的 `ConfigureServices` 还没跑):

```csharp
Configure<SnowOpenApiOptions>(options =>
{
    options.Enabled = true;
    options.Title = "Snow API";
    options.Version = "v1";
    options.Description = "订单与商品接口";
    options.RoutePrefix = "openapi";
    options.IncludeXmlComments = true;
    options.AddJwtBearerSecurity = true;
});
```

`IncludeXmlComments` 打开时会自动找各模块程序集目录下的 `{程序集名}.xml`(生成项目需开 `GenerateDocumentationFile`), 把三行 summary 写成接口摘要、把 `<param>` 写成参数说明、把属性的 summary 写成字段说明。`AddJwtBearerSecurity` 在文档里加 `Bearer` 方案, 界面上可直接填令牌(只填令牌本身, 不加 `Bearer ` 前缀)调试。

文档名就是 `Version`, 所以对外地址是 `/{RoutePrefix}/{Version}.json`(默认 `/openapi/v1.json`)。

## 健康检查

微软原生的 `AddHealthChecks` / `MapHealthChecks` 已经够薄, 框架不再包一层: 检查项与端点都由宿主自己登记。

```csharp
builder.Services.AddHealthChecks()
    .AddCheck<DbHealthCheck>("db", tags: ["ready"]);

WebApplication app = builder.Build();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    // 探针默认匿名: 只回总体状态与耗时, 不带检查项明细
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json; charset=utf-8";
        await context.Response.WriteAsJsonAsync(new
        {
            status = report.Status.ToString(),
            totalDuration = report.TotalDuration.TotalMilliseconds,
        });
    },
});
```

要做 `live` / `ready` 分离就映射两次, 用 `Predicate` 按 tag 过滤: `MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false })` 只要进程活着, `/health/ready` 才是依赖全通。

不提供框架包的原因: `AddHealthChecks` 返回的 `IHealthChecksBuilder` 本身就是登记表, 再包一层 registry 只是把同一份登记记两遍, 还多一条“必须早于 `AddApplication`”的约束; 而默认 `RequireAuthorization = false` + `IncludeDetails = true` 恰好是匿名拿到全部检查项名、耗时与 `exception.Message` 的最坏组合。

## SignalR

```csharp
[SnowHub("/signalr/chat")]        // 路径可省, 省了按类名算: ChatHub → /signalr/chat
public sealed class ChatHub : Hub { }
```

```csharp
// Program.cs: 两个入口都必须在 AddApplication 之前调用
builder.Services.AddSnowHub<ChatHub>();   // 方式一: 手动登记(不带特性 / 自定义路径时用)
builder.Services.AddSnowSignalR(options => options.EnableDetailedErrors = true);   // 方式二: 调整选项; Hub 扫描默认开启
```

Hub 端点由 `SnowSignalRModule` 在 `ConfigureServices` 阶段读注册表并登记映射, 所以登记动作必须发生在 `AddApplication` 之前, 之后追加的 Hub 不会被映射。

```csharp
app.Run();   // Hub 端点由模块登记, 框架统一执行
```

单独使用本包时才需要手写 `app.MapSnowHubs();`。

选项不读配置节(与 ABP 的 `AbpSignalROptions` 一样只用代码), 全部经 `AddSnowSignalR` 调整:

```csharp
builder.Services.AddSnowSignalR(options =>
{
    options.Enabled = true;                      // 关掉只登记服务, 不映射端点
    options.AutoDiscoverHubs = true;             // 扫各模块程序集里的 Hub 派生类
    options.EnableDetailedErrors = false;
    options.MaximumReceiveMessageSize = 32768;
});
```

## 限流

直接用微软自带的限流中间件(`Microsoft.AspNetCore.RateLimiting`), 本包只做两件事: 在模块里帮你调用 `AddRateLimiter`, 并提供 `UseSnowRateLimiting()` 让你挂进管道。**没有任何自有选项类型**: 配置动作收到的就是原生的 `RateLimiterOptions`。

```csharp
// Program.cs: 配置动作必须在 AddApplication 之前交给框架, 真正下发在 SnowRateLimitingModule 里
builder.Services.AddSnowRateLimiting(limiter =>
{
    // 微软默认 503, 要 429 必须自己设
    limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // 全局限流: 每 IP 每 60 秒 100 个请求
    limiter.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromSeconds(60),
                QueueLimit = 0,
                AutoReplenishment = true,
            }));

    // 命名策略: 端点上用 [EnableRateLimiting("strict")] / RequireRateLimiting("strict") 引用
    limiter.AddFixedWindowLimiter("strict", options =>
    {
        options.PermitLimit = 5;
        options.Window = TimeSpan.FromSeconds(60);
        options.QueueLimit = 0;
        options.AutoReplenishment = true;
    });

    // 其他原生写法同样可用
    limiter.AddSlidingWindowLimiter("sliding", options =>
    {
        options.PermitLimit = 10;
        options.Window = TimeSpan.FromSeconds(10);
        options.SegmentsPerWindow = 2;
    });
    limiter.AddTokenBucketLimiter("burst", options =>
    {
        options.TokenLimit = 20;
        options.TokensPerPeriod = 5;
        options.ReplenishmentPeriod = TimeSpan.FromSeconds(10);
    });
    limiter.AddConcurrencyLimiter("concurrent", options => options.PermitLimit = 4);
});

builder.Services.AddApplication<MyModule>();

[DependsOn(typeof(SnowRateLimitingModule))]
public sealed class MyModule : SnowModule;
```

```csharp
app.UseSnowRateLimiting();   // 宿主在管道里挂, 必须在 UseRouting() 之后
```

`UseSnowRateLimiting()` 内部就是 `app.UseRateLimiter()`; 它必须排在 `UseRouting()` 之后 —— 路由之前读不到端点元数据, 请求会全部放行且不报错(见「中间件管道」的顺序约束)。

说明:

- `AddSnowRateLimiting(...)` 必须在 `AddApplication` 之前调用, 动作只是被收集, 真正下发在模块的 `ConfigureServices` 里。
- 不写任何配置动作也能用: 模块照样登记 `AddRateLimiter`, 端点上的 `[EnableRateLimiting]` 与 `RequireRateLimiting` 不报错。
- 面向业务的细粒度限流(按手机号、按用户、按操作名)微软这套中间件不负责, 需要自己在应用服务里做计数; 那属于 ABP 的 Pro 模块 `Operation Rate Limiting`, 本包不涉及。
- 因此没有配置节: `RateLimiterOptions` 里有 `PartitionedRateLimiter` 与 `OnRejected` 委托, JSON 表达不了, 全部用代码写。

## 跨域(CORS)

完全照 ABP: 框架不代劳, 宿主模块自己写。ABP 在 `Volo.Abp.AspNetCore` 里只有一个 `WithAbpExposedHeaders()`(暴露它自己的错误格式与租户解析错误两个响应头), 策略与中间件都写在宿主; Snow 没有这类自定义响应头, 所以框架侧一行不写, 配置键与策略内容跟 ABP 模板逐项一样。

```json
{
  "App": {
    "CorsOrigins": "https://app.example.com,https://*.example.com"
  }
}
```

```csharp
// 宿主模块 ConfigureServices, 与 ABP 模板 ConfigureCors 逐字对应
context.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder
            .WithOrigins(
                configuration["App:CorsOrigins"]?
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(origin => origin.TrimEnd('/'))
                    .ToArray() ?? []
            )
            .SetIsOriginAllowedToAllowWildcardSubdomains()
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// 管道位置: 需要落在路由之后、认证之前(CORS 要看端点元数据, 又要在授权拒绝之前给出响应头)
app.UseCors();
```

`UseRouting()` / `UseAuthentication()` / `UseAuthorization()` 都由你在管道里按行序写, 不交给 WebApplication 自动插入 —— 自动插入会把它们放到管道最前面, 要读用户的中间件就全都跑在认证之前。

**CORS 直接写进管道**: 它需要"路由之后、认证之前", 就在 `app.UseRouting()` 与 `app.UseAuthentication()` 之间写 `app.UseCors("MyPolicy")`; 需要端点级 `[EnableCors]` 就写无参 `app.UseCors()` 让中间件按端点策略放行。

`configuration` 就是 `context.Configuration`(或 `context.Services.GetConfiguration()`)。需要额外暴露响应头时在 `AllowCredentials()` 之前加 `.WithExposedHeaders("你的头")` —— 对应 ABP 的 `WithAbpExposedHeaders()`。

## 配置项: 定义一次, 值从哪来由来源决定

配置项系统(对应 ABP 的 Settings)把"参数名"与"值存在哪"分开: 能力包只声明定义, 宿主在 `Settings` 节或别的来源给值。同一个名字可以不改代码就换成数据库、租户设置。

### 1. 声明与读取

```csharp
public class AcmeSettingProvider : SettingDefinitionProvider
{
    public override void Define(ISettingDefinitionContext context)
    {
        context.Add(
            new SettingDefinition("Acme.Smtp.Host", "127.0.0.1", "SMTP 地址"),
            new SettingDefinition("Acme.Smtp.Password", null, "SMTP 密码", isEncrypted: true));
    }
}

[DependsOn(typeof(SnowSettingsModule))]
public class AcmeModule : SnowModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<SnowSettingOptions>(options => options.DefinitionProviders.Add<AcmeSettingProvider>());
    }
}
```

读取用 `ISettingProvider`: `GetOrNullAsync(name)` 取不到返回 null, `GetAsync(name)` 取不到抛异常; 结果按定义表走完所有来源, 加密项自动解密。

### 2. 值从哪来

| 来源 | 读什么 |
| --- | --- |
| `ConfigurationSettingValueProvider`(默认, 名字 `C`) | `Settings:{配置项名}`, 例如 `"Settings": { "Acme.Smtp.Host": "127.0.0.1" }` |

`SettingProvider` 按登记顺序**倒着**问每个来源, 第一个给出非 null 值的获胜, 便于宿主后登记覆盖框架默认; `SettingDefinition.Providers` 可以限定某个配置项只问指定来源。接数据库/租户设置就是自己实现 `ISettingValueProvider` 再 `options.ValueProviders.Add<T>()`。

### 3. 加密项

定义上 `isEncrypted: true` 时, 配置里放密文, 读取时由 `ISettingEncryptionService` 解密。密文用 `IStringEncryptionService`(`Snow.Security`, AES/CBC + PBKDF2-SHA1, 与 ABP 同算法同默认参数)生成:

```csharp
IStringEncryptionService cipher = serviceProvider.GetRequiredService<IStringEncryptionService>();
string cipherText = cipher.Encrypt("p@ssw0rd")!;   // 放进 Settings:Acme.Smtp.Password
```

解密失败(口令换过、值其实是明文)时按 `SnowSettingOptions.ReturnOriginalValueIfDecryptFailed` 处理: 默认 true 记一条警告并返回原文, 设 false 返回空串。生产环境务必换掉 `SnowStringEncryptionOptions.DefaultPassPhrase`。

### 4. 邮件与 FTP 就是这套系统的头两个用户

邮件没有自己的配置节, 9 个键都在配置项系统里(对应 ABP 的 `Abp.Mailing.*`):

| 键 | 默认值 | 说明 |
| --- | --- | --- |
| `Snow.Mailing.Smtp.Host` | `127.0.0.1` | 服务器地址 |
| `Snow.Mailing.Smtp.Port` | `25` | 端口 |
| `Snow.Mailing.Smtp.UserName` | 无 | 登录账号, 仅 `UseDefaultCredentials=false` 时读 |
| `Snow.Mailing.Smtp.Password` | 无(加密) | 登录密码, 放 `IStringEncryptionService` 的密文 |
| `Snow.Mailing.Smtp.Domain` | 无 | 认证域 |
| `Snow.Mailing.Smtp.EnableSsl` | `false` | true 走 465 直连 SSL, false 走 STARTTLS |
| `Snow.Mailing.Smtp.UseDefaultCredentials` | `true` | true 匿名投递(内网中继、MailHog), 不认证 |
| `Snow.Mailing.DefaultFromAddress` | `noreply@snow.io` | 默认发件人地址 |
| `Snow.Mailing.DefaultFromDisplayName` | `Snow application` | 默认发件人显示名 |

```json
{
  "Settings": {
    "Snow.Mailing.Smtp.Host": "localhost",
    "Snow.Mailing.Smtp.Port": "1025",
    "Snow.Mailing.Smtp.EnableSsl": "false",
    "Snow.Mailing.Smtp.UseDefaultCredentials": "true",
    "Snow.Mailing.DefaultFromAddress": "no-reply@snow.local",
    "Snow.Mailing.DefaultFromDisplayName": "Snow Sample"
  }
}
```

每次发信前现读一遍, 改配置不用重启。`EmailOptions` 只剩 Snow 自己的两个开关: `Timeout`(单封整体超时, 默认 100 秒)与 `IsBodyHtml`(正文默认格式)。

FTP 同样没有配置节, 10 个键是 `Snow.Ftp.*`(对应 ABP 的 `Abp.Ftp.*`), 密码同样标了 `IsEncrypted`:

| 键 | 默认值 | 说明 |
| --- | --- | --- |
| `Snow.Ftp.Host` | 无 | 服务器地址, 必填: 留空时第一次使用就抛 `配置项 Snow.Ftp.Host 的值不能为空。` |
| `Snow.Ftp.Port` | `21` | 端口, 隐式 FTPS 一般是 990 |
| `Snow.Ftp.UserName` | 无 | 登录账号, 留空按匿名登录(用户名 `anonymous`) |
| `Snow.Ftp.Password` | 无(加密) | 登录密码, 放 `IStringEncryptionService` 的密文 |
| `Snow.Ftp.UseSsl` | `false` | true 走显式 FTPS(`AUTH TLS`) |
| `Snow.Ftp.EncodingName` | `utf-8` | 文件名编码, 中文乱码时改这里(如 `gb2312`) |
| `Snow.Ftp.ConnectTimeout` | `10000` | 连接超时(毫秒) |
| `Snow.Ftp.ReadTimeout` | `30000` | 读写超时(毫秒) |
| `Snow.Ftp.IgnoreCertificateErrors` | `false` | 仅自签名证书的内网 FTP 设为 true |
| `Snow.Ftp.RetryAttempts` | `3` | 失败重试次数 |

```json
{
  "Settings": {
    "Snow.Ftp.Host": "10.0.0.8",
    "Snow.Ftp.UserName": "deploy",
    "Snow.Ftp.Password": "IStringEncryptionService 生成的密文"
  }
}
```

`FtpOptions` 已经不存在了: `FluentFtpClient` 每次操作前通过 `IFtpConfiguration` 现读这些键(所以改配置不用重启), 你要换来源(数据库、密钥服务)就自己实现 `ISettingValueProvider` 或 `IFtpConfiguration`。

## 工作单元与领域事件: 保存时采集, 提交前派发

`SnowDbContext` 把"存库"和"发事件"拆成两步: 写库之前把聚合根上挂的事件(`AddDomainEvent`)采集下来并清空, 写库之后再决定把事件交给谁。这样事件一定对应真的落库的数据, 不会因为保存失败而白发一次。

| 场景 | 行为 |
| --- | --- |
| 不在工作单元里(`SaveChanges` 直接调) | 保存成功后立即派发 |
| 在工作单元里 | 只登记进 `IUnitOfWork.LocalEvents`, 等 `CompleteAsync` |

`CompleteAsync` 的顺序(与 ABP 的工作单元一致):

1. 提交前回调: 上下文刷库, 把缓冲区里的改动写进事务
2. 派发本地事件队列, 按 `EventOrder` 升序; 处理器里再改数据、再 `SaveChanges`, 又会攒出新事件
3. 每次派发完再刷一次库, 直到队列为空
4. 提交事务
5. 提交后回调

所以处理器改的数据和引发它的事件在同一个事务里: 处理器抛异常, 整个工作单元回滚, 不会出现"事件发了、数据没落"。

```csharp
IUnitOfWork unitOfWork = unitOfWorkManager.Begin();

await orderRepository.InsertAsync(new Order(...));  // 事件入队, 还没派发
await unitOfWork.CompleteAsync();                   // 事务内派发事件 → 提交
```

不想显式管就用 `[Transactional]`: 方法正常返回时提交, 抛异常时回滚。

派发链是 `IUnitOfWorkEventPublisher` → `IEventBus`, 处理器写法不变(`IEventHandler<TEvent>`)。事件对象在编译期只知道 `object` 时(工作单元正是这种情况)用非泛型重载:

```csharp
await eventBus.PublishAsync(typeof(OrderCreatedEvent), orderCreatedEvent, cancellationToken);
```

既没有工作单元、也没有登记事件总线时, 采集到的事件无处可去会被丢弃; 需要可靠投递就用 `Snow.Messaging`。

## 框架服务: 校验 / 异常

这批包的关系是"契约 + 默认实现", 引进来就可用, 要换后端再加实现包或改配置。

| 需求 | 用法 |
| --- | --- |
| 参数校验 | `[SnowValidation]` 标在应用服务或方法上, 复杂参数按 DataAnnotations 校验, 有错抛 `SnowValidationException`; 再加 `Snow.Validation.FluentValidation` 则同一处同时跑 FluentValidation 规则 |
| 对象映射 | 注入 `IObjectMapper`(`Map<OrderDto>(order)` / `Map(order, dto)`); `Snow.ObjectMapping.Mapster` = ABP AutoMapper 的 Mapster 版: `SnowMapsterProfile` + `[SnowAutoMap]`, 模块自动扫描, 启动即编译校验 |
| 异常翻译 | `app.Services.ToErrorResponse(exception)` 得到统一错误对象; 业务异常用 `BusinessException`(或语义更强的 `UserFriendlyException`)可带 `Code`、`Details` 说明文本, 结构化上下文用 `WithData(键, 值)` |
| 本地化 | `[LocalizationResource("MyApp")]` 标资源类, 注入 `IStringLocalizer<MyAppResource>` 取字符串 |
| JSON | 注入 `ISnowJsonSerializer`, 选项与框架其余部分一致, 不要在业务里另建 `JsonSerializerOptions`; `Snow.Json` 自带 System.Text.Json 实现, 要 Newtonsoft 行为再加 `Snow.Json.Newtonsoft` |

### 异常类型体系(对齐 ABP)

| 类型 | 基类 | 日志级别 | 用途 |
| --- | --- | --- | --- |
| `SnowException`(`Snow.Exceptions`) | `Exception` | — | 框架异常树的根(对应 ABP 的 `AbpException`): 只有三个构造器, 不携带错误码与日志级别; 需要它们的子类自己实现 `IHasErrorCode` / `IHasLogLevel`, 取级别用 `GetLogLevel()` 扩展方法; 与 `BusinessException` 树平行 |
| `BusinessException` | `Exception` | Warning | 业务规则不允许; 直接继承 `Exception`(与 ABP 的 `Volo.Abp.BusinessException` 同形, 不在 `SnowException` 树下); 实现标记接口 `IBusinessException`; 属性 `Code` / `Details`(`string`, 错误说明文本)/ `LogLevel`(默认 Warning), 构造器与 ABP 同形(全部可选); `WithData(键, 值)` 把结构化上下文写进 `Exception.Data`, 序列化进错误响应的 `data` |
| `UserFriendlyException` | `BusinessException` | Warning | 同上, 语义上承诺消息可直接展示给调用方; 实现标记接口 `IUserFriendlyException` |
| `EntityNotFoundException`(在 `Snow.ExceptionHandling`, 命名空间仍是 `Snow.Domain.Entities`) | `SnowException` | Error | 仓储 `GetAsync` 找不到实体时抛; 与 ABP 11 一样只携带实体类型与主键, 不带错误码/状态码实现, 由异常处理层按类型判成 404 + 本地化消息 |
| `SnowValidationException` / `SnowJsonException` / `SnowObjectMappingException` / `SnowWorkflowException` / `SnowTenantNotFoundException` / `SnowHttpServiceException` / `SnowInitializationException` | `SnowException` | Error(校验为 Warning) | 各自场景的失败; 需要错误码或非默认级别的子类自行实现 `IHasErrorCode` / `IHasLogLevel`(`SnowValidationException` 就是 Warning) |

异常处理层(中间件 / MVC 过滤器 / 转换器)只按接口取信息: 错误码取 `IHasErrorCode.Code`, 日志级别取 `IHasLogLevel.LogLevel`, 错误说明取 `IHasErrorDetails.Details`(`string`), 结构化上下文取 `Exception.Data`, HTTP 状态码取 `IHasHttpStatusCode.HttpStatusCode`(`Snow.Exceptions`, 与 ABP 最新的同名接口同形, 值是 400 / 404 / 409 这样的数字), 不再按具体类型硬编码。状态码判定顺序是先看 `IHasHttpStatusCode`, 再按种类兜底 —— 校验与业务异常 400, `UnauthorizedAccessException` 403, `KeyNotFoundException` 404, `NotImplementedException` 501, 其余 500(与 ABP 的差异: ABP 把 `IBusinessException` 映射成 403, Snow 用 400, 语义更贴切)。`Snow.Exceptions.IBusinessException`(`IUserFriendlyException` 派生自它)是两个零成员标记接口, 与 ABP 同名接口一一对应: 异常处理层把实现了它们的异常一律当业务失败处理(400 + `Code`), 所以第三方自定义异常只要实现接口、不必继承 `BusinessException`, 也能被正确映射。领域层想自己声明 404 就在异常上实现这个接口, 不必让 `Snow.AspNetCore` 反向依赖 `Snow.Domain`。

## 多租户

两个包, 与 ABP 的 `Volo.Abp.MultiTenancy` / `Volo.Abp.AspNetCore.MultiTenancy` 一一对应:

- `Snow.MultiTenancy` —— 内核, 与环境无关: 当前租户上下文、租户配置、租户存储、解析出资者
- `Snow.AspNetCore.MultiTenancy` —— Web 适配层: 四个请求侧出资者 + 中间件

默认**关闭**(与 ABP 一致): 不开的时候 `ICurrentTenant.Id` 恒为 `null`, 中间件原样放行, 代码照常跑。

### 打开与取用

```csharp
[DependsOn(typeof(SnowAspNetCoreMultiTenancyModule))]
public sealed class MyHttpApiHostModule : SnowModule;

// 在启动模块的管道里挂多租户(认证之后、授权之前), 见「中间件管道」
public override void OnApplicationInitialization(ApplicationInitializationContext context)
    => context.GetApplicationBuilder().UseSnowMultiTenancy();
```

开关不读配置(与 ABP 一致), 在启动模块里用代码打开; 四个请求侧出资者读的键名默认 `__tenant`, 要改也是代码:

```csharp
Configure<SnowMultiTenancyOptions>(options => options.IsEnabled = true);
Configure<SnowAspNetCoreMultiTenancyOptions>(options => options.TenantKey = "__tenant");
```

租户清单则整体绑定**配置根**(与 ABP 一样, 模块不嗅探配置节是否存在), 直接写在根的 `Tenants` 里:

```json
{
  "Tenants": [
    { "Id": "11111111-1111-1111-1111-111111111111", "Name": "Acme" },
    { "Id": "22222222-2222-2222-2222-222222222222", "Name": "Beta" },
    { "Id": "33333333-3333-3333-3333-333333333333", "Name": "Old", "IsActive": false }
  ]
}
```

- 取当前租户: 注入 `ICurrentTenant`, 读 `Id` / `Name` / `IsAvailable`; 手动切换用 `using (CurrentTenant.Change(tenantId, "名称")) { ... }`, 支持嵌套, 内层退出自动还原外层(异步链内可见)
- 租户配置: `TenantConfiguration`(`Id` / `Name` / `IsActive` / `ConnectionStrings`, 默认键 `Default`)

### 租户从哪来(出资者顺序即优先级)

| 顺序 | 出资者 | 来源 |
| --- | --- | --- |
| 1 | `CurrentUserTenantResolveContributor` | 已登录用户的 `tenantid` 声明(用户身份最可信, 前端改 query 也越不了权) |
| 2 | `QueryStringTenantResolveContributor` | `?__tenant=acme` |
| 3 | `RouteTenantResolveContributor` | 路由值 `{__tenant}` |
| 4 | `HeaderTenantResolveContributor` | 请求头 `__tenant` |
| 5 | `CookieTenantResolveContributor` | Cookie `__tenant` |

键名统一由 `MultiTenancy:AspNetCore:TenantKey` 决定(默认 `__tenant`)。自己再加出资者: 实现 `ITenantResolveContributor`, 在模块里 `context.Services.AddTransient<ITenantResolveContributor, MyContributor>()` —— 排在哪一位就决定了它的优先级。

中间件流程: 关闭 → 放行; 开启 → 跑出资者 → 解析出值就去存储查 → 查到则 `ICurrentTenant.Change` 包住整个请求, 查不到直接返回 **404** `{"error":{"code":"TenantNotFound",...}}`(不会悄悄当宿主处理)。

多租户中间件由宿主在管道里挂在 `UseAuthentication()` 之后、`UseAuthorization()` 之前 —— 认证之后路由值读得到、已认证用户也读得到; 挂到认证之前, "当前用户"出资者拿不到租户, 会静默回落到 Host 租户。

### 租户存储

- 默认 `DefaultTenantStore`: 从 `MultiTenancy:Tenants` 读, 名称比较去掉首尾空白且忽略大小写, `IsActive: false` 的租户当作不存在
- 换数据库实现: `context.Services.Replace(ServiceDescriptor.Singleton<ITenantStore, MyTenantStore>())`; 契约是 `FindAsync` / `GetAsync` 各两个重载
- 自己解析并翻译: `ITenantConfigurationProvider.GetAsync(saveResolveResult: true)` 一步拿到 `TenantConfiguration`, 结果同时写进 `ITenantResolveResultAccessor`

### 实体隔离

实体实现 `IMultiTenant`(`Guid? TenantId`), 交给数据过滤按当前租户做隔离。

## 对象映射 / 校验后端 / JSON 后端

这三组都是"契约 + 实现包"结构: 契约包引进来就有静默实现或默认实现, 要真能力再引一个实现包, 代码里的注入点不变。

### 对象映射(Mapster, 对应 ABP 的 AutoMapper)

ABP 用 AutoMapper + Profile + [AutoMap]; Snow 用 Mapster, 逐个能力对齐:

| ABP | Snow |
| --- | --- |
| `Volo.Abp.AutoMapper` 的 `AbpAutoMapperModule` | `Snow.ObjectMapping.Mapster` 的 `SnowObjectMappingMapsterModule` |
| `Profile : MapperProfile` 里的 `CreateMap<,>()` | `SnowMapsterProfile` 里的 `CreateMap<,>()` / `CreateTwoWays<,>()` |
| `[AutoMap(typeof(Dto))]` / `[AutoMap(AutoMapDirection.From, ...)]` | `[SnowAutoMap(typeof(Dto))]` / `[SnowAutoMap(SnowAutoMapDirection.From, ...)]` |
| `options.AddProfile<T>()` / `AddMaps<TModule>()` | 不用登记 —— 模块扫描模块程序集里的 `SnowMapsterProfile` / `IRegister` |
| `Configurators` / `DefaultMaxDepth = 64` / `ValidatingProfiles` + `AssertConfigurationIsValid` | 固定 `DefaultMaxDepth = 64` + 启动即 `Compile()` 校验(失败包成 `SnowObjectMappingException`); 要追加配置就写 `SnowMapsterProfile`, 或在容器里登记 `IRegister` |
| 注入 AutoMapper 的 `IMapper` | 注入 `MapsterMapper.IMapper` |

```csharp
// 宿主不需要任何登记: 引包 + DependsOn 就行, 模块扫描模块程序集里的 Profile 自动应用
[DependsOn(typeof(SnowObjectMappingMapsterModule))]
public sealed class MyModule : SnowModule;

public sealed class OrderMappingProfile : SnowMapsterProfile
{
    public OrderMappingProfile()
    {
        CreateMap<Order, OrderDto>();
        CreateMap<Order, OrderSummary>(setter => setter.Map(destination => destination.Label, source => source.Name + "!"));
        CreateTwoWays<Order, OrderInput>();
    }
}

[SnowAutoMap(typeof(OrderDto))]                                  // 默认 To: 自身 -> OrderDto
[SnowAutoMap(SnowAutoMapDirection.From, typeof(OrderInput))]     // From: OrderInput -> 自身
public class Order
{
    public string? Name { get; set; }
}

// 不放在模块程序集里(比如只在 Program.cs 里加), 就登记成 IRegister: 建配置时一并应用
builder.Services.AddSingleton<IRegister, ExtraMappingRegister>();
```

- 注入 `IObjectMapper`: `Map<OrderDto>(order)` 映射成新对象, `Map(order, dto)` 就地填充已有对象, `Map(source, typeof(Order), typeof(OrderDto))` 走运行时类型
- 模块没有选项、不读配置节: 固定扫描模块程序集里的 `IRegister`(含 `SnowMapsterProfile` 子类)与 `[SnowAutoMap]`, 固定 `DefaultMaxDepth = 64`, 并在启动时 `Compile()` 全部映射 —— 映射写错应用直接起不来(`映射配置校验失败: ...`), 而不是等第一次 `Map` 才炸
- 扫描之外只有一条口子: 把 `IRegister` 登记进容器(`services.AddSingleton<IRegister, MyRegister>()`), 模块建配置时按类型去重后一并 `Register`
- 不引实现包时 `IObjectMapper` 是 `NullObjectMapper`, 调用即抛"请引入 Snow.ObjectMapping.Mapster 包"

### 校验(FluentValidation)

- `Snow.Validation`: DataAnnotations 校验 + `[SnowValidation]` 方法级拦截
- `Snow.Validation.FluentValidation`(对应 ABP 的 Volo.Abp.FluentValidation): 实现 `AbstractValidator<T>` 即自动登记, 规则与 ABP 的 `AbpFluentValidationConventionalRegistrar` 一致 —— 瞬态生命周期, 同时暴露具体校验器类型与 `IValidator<T>`(可以直接注入 `CreateOrderDtoValidator`), 标了 `[DisableConventionalRegistration]` 的跳过, 也可用 `AddSnowFluentValidation(o => { o.ScanValidators = false; o.ValidatorTypes.Add(typeof(XxxValidator)); })` 只点名登记; 并把 `ISnowObjectValidator` 换成"先 DataAnnotations 再 FluentValidation, 错误合成一份"的管线
- MVC 侧的自动校验不另建包: `SnowValidationActionFilter` 就在 `Snow.AspNetCore.Mvc` 里(对应 ABP 把 `AbpValidationActionFilter` 放在 Volo.Abp.AspNetCore.Mvc)。它用 `IModelStateValidator` 校验 ModelState, 再用 `IMethodInvocationValidator` 校验 Controller(含自动 API)的复杂参数, 不通过抛 `SnowValidationException`, 由统一异常处理转成 400 + `ValidationError`, 过滤器顺序 -1000(最前), 开关是 `Mvc:AutoModelValidation`。因此换成 FluentValidation 后 MVC 侧行为自动跟着变, 不需要再加任何包。

```csharp
[DependsOn(typeof(SnowValidationFluentValidationModule))]
public sealed class MyModule : SnowModule { }

public sealed class CreateOrderDtoValidator : AbstractValidator<CreateOrderDto>
{
    public CreateOrderDtoValidator()
    {
        RuleFor(dto => dto.Amount).GreaterThan(0).WithMessage("金额必须大于 0");
    }
}
```

选项 `SnowFluentValidationOptions`(`Enabled` / `ScanValidators` / `ValidateDataAnnotations` / `ValidatorTypes`, 不读配置节, 宿主在 `PreConfigureServices` 里给)。MVC 自动校验的开关是 `SnowAspNetCoreMvcOptions.AutoModelValidation`(默认 true, 用 `PostConfigure` 改)。

### JSON(默认 System.Text.Json, 可换 Newtonsoft.Json)

`Snow.Json` 一个包搞定两件事: 契约与公共选项(`WriteIndented` / `IgnoreNullValues` / `AllowComments`) + **默认的 System.Text.Json 实现**(小驼峰、忽略 null、枚举转小驼峰字符串、允许注释与尾随逗号, 零第三方依赖), 装了就生效, 不需要再加别的包:

```csharp
[DependsOn(typeof(SnowJsonModule))]
public sealed class MyModule : SnowModule { }
```

要 Newtonsoft.Json 的行为(比如保留 `$type`)时才加装:

```bash
dotnet add package Snow.Json.Newtonsoft
```

```csharp
[DependsOn(typeof(SnowJsonNewtonsoftModule))]   // 以 ReplaceServices 顶替内置的 System.Text.Json 实现
public sealed class MyModule : SnowModule { }
```

两个实现登记同一个 `ISnowJsonSerializer`(注入点不变), 两个都装时按模块加载顺序, 后者胜。

选项 `SnowJsonOptions`(`WriteIndented` / `IgnoreNullValues` / `AllowComments` / `InputDateTimeFormats` / `OutputDateTimeFormat`, 不读配置节, 宿主在 `PreConfigureServices` 里给); 两个实现都没有独立选项, 各自的库级默认值(驼峰、枚举转字符串、日期处理等)写在序列化器里, 要换转换器就自己注入 `ISnowJsonSerializer`。

## 本地打包

```bash
# 全部包输出到仓库根的 .nuget-packages 目录(同时产出 .snupkg 符号包)
powershell -ExecutionPolicy Bypass -File ./build/pack.ps1

# 指定版本号
powershell -ExecutionPolicy Bypass -File ./build/pack.ps1 -Version 1.2.0
```

推送到 nuget.org 用 `build/push.ps1 -ApiKey $env:NUGET_API_KEY`; 本地联调时把
`build/nuget.local.config` 复制成消费者项目的 `nuget.config` 即可从 `.nuget-packages` 目录还原。

## CI 发布(GitHub Actions)

`.github/workflows/publish.yml` 在推 `v*` 标签或手动触发时打包并发布, 认证走
nuget.org 的 Trusted Publishing: 用 OIDC 换 1 小时有效的临时 Key, 仓库里不存任何密钥。

nuget.org 侧只需配一条策略(用户名 → Trusted Publishing → Add policy):

| 字段 | 值 |
| --- | --- |
| Repository Owner | `jamen-jiang` |
| Repository | `Snow` |
| Workflow File | `publish.yml`(只写文件名, 不含 `.github/workflows/`) |
| Environment | 留空 |

可选: 在仓库 Secrets 里加 `NUGET_USER`(nuget.org 的 profile 名, 不是邮箱), 不加则用
工作流里的默认值。发版就是打标签:

```bash
git tag v1.2.0 && git push origin v1.2.0
```

## 包一览

### 内核与宿主

| 包 | 用途 |
| --- | --- |
| `Snow` | 名称占位包: 不含代码, 只引用 `Snow.Core`(与 ABP 的 `Volo.Abp` 同形) |
| `Snow.Core` | 内核: 模块系统、约定式依赖注入、AOP 拦截契约(`AspectContext` / `AspectDelegate` / `SnowInterceptorAttribute` / `SnowProxyHelper`)、表达式求值、System 扩展方法 |
| `Snow.Data` | 数据过滤: 按过滤器类型开关全局过滤(软删除等), 状态按异步调用链隔离 |
| `Snow.Timing` | 时间: `IClock` 抽象与直接读系统时钟的默认实现 |
| `Snow.Tracing` | 链路标识: `ICorrelationIdProvider` 与进程内默认实现 |
| `Snow.AspNetCore` | Web 宿主能力包: `SnowAspNetCoreModule`(HTTP 上下文访问器、Web 对象访问器) + `GetHostingEnvironment()`(对应 ABP 的 `AbpAspNetCoreServiceCollectionExtensions`) + 宿主显式初始化 `app.InitializeApplicationAsync()`(对应 ABP 的 `AbpApplicationBuilderExtensions.InitializeApplicationAsync`, 驱动模块生命周期; 管道由宿主在启动模块里手写) + 端点路由选项 `SnowEndpointRouterOptions` 与宿主最后一行 `UseSnowEndpoints()`(对应 ABP 的 `AbpEndpointRouterOptions` / `UseConfiguredEndpoints`) + `UseSnowCorrelationId()` 与链路标识中间件 `SnowCorrelationIdMiddleware`(对应 ABP 的 `AbpCorrelationIdMiddleware`) + 应用初始化上下文扩展 `GetApplicationBuilder()` 一族; 不含任何具体后端(EF / Redis / Hangfire / Serilog / MailKit / FluentFTP / ClosedXML / Elsa 由宿主按需引用) |
| `Snow.AspNetCore.Serilog` | Serilog 集成: 请求级租户/用户/链路标识自动进日志上下文; 已依赖 `Snow.AspNetCore`(与 ABP 的 `Volo.Abp.AspNetCore.Serilog` 依赖 `Volo.Abp.AspNetCore` 同形); 中间件由宿主在管道里调 `UseSnowSerilogEnrichers()` |
| `Snow.AspNetCore.Mvc` | MVC 与远程服务约定: 实现 `IRemoteService` / 标 `[RemoteService]` 的 Controller 按约定生成路由与动词(路由/动词/参数绑定), 正常返回值不包装, 异常与模型校验统一成 400/403/404/500 错误响应 |
| `Snow.OpenApi` | OpenAPI 文档与调试界面: 官方 Microsoft.AspNetCore.OpenApi 出文档 + Scalar 出界面, 自动收录全部自动 API, 可选 Bearer 认证方案, 自动加载程序集 XML 注释 |
| `Snow.AspNetCore.SignalR` | SignalR: `SnowHub` 基类与 `[SnowHub]` 路径注解, Hub 自动扫描, `MapSnowHubs()` 挂端点 |
| `Snow.AspNetCore.RateLimiting` | 限流: 对微软自带 `AddRateLimiter`/`UseRateLimiter` 的薄封装(`UseSnowRateLimiting()`, 宿主在管道里调, 须在 `UseRouting()` 之后), 无自有选项 |
| `Snow.AspNetCore.MultiTenancy` | 多租户 Web 适配层: 查询串 / 路由 / 请求头 / Cookie 四个租户出资者 + 多租户中间件(`UseSnowMultiTenancy()`, 宿主在管道里调, 须在 `UseAuthentication()` 之后, 请求级切换租户, 未知租户回 404), 已依赖 `Snow.MultiTenancy` |

### 领域与应用

| 包 | 用途 |
| --- | --- |
| `Snow.Domain` | 实体、聚合根、审计实体、软删除、领域事件、仓储契约、值对象 |
| `Snow.Application` | 应用层契约: 实体 DTO、分页请求与分页结果、`IQueryable.PageBy(IPagedResultRequest)` 分页扩展(对应 ABP AbpPagingQueryableExtensions); 业务应用服务写在消费方项目里 |
| `Snow.Uow` | 工作单元与事务: `IUnitOfWork`、`[Transactional]` 注解、提交/回滚回调、领域事件队列(提交事务前按序派发) |

### 持久化

| 包 | 用途 |
| --- | --- |
| `Snow.EntityFrameworkCore` | EF Core 集成: `SnowDbContext` 基类、连接串解析、按工作单元共享的上下文、ABP 式仓储、软删除/领域事件拦截 |
| `Snow.EntityFrameworkCore.SqlServer` | SQL Server 提供程序 |
| `Snow.EntityFrameworkCore.Sqlite` | SQLite 提供程序(单元测试与本地开发) |
| `Snow.EntityFrameworkCore.MySql` | MySQL 提供程序。因依赖只有 Oracle 官方 preview 通道, 该包版本自带 `-preview.N` 后缀 |
| `Snow.EntityFrameworkCore.PostgreSql` | PostgreSQL 提供程序(基于 Npgsql 官方提供程序) |

### 基础设施

| 包 | 用途 |
| --- | --- |
| `Snow.EventBus` | 事件总线: 事件契约(含处理器接口)、发布策略、处理器包装与模块 |
| `Snow.Messaging` | 消息总线: 命令/查询契约(含管道行为接口)与默认派发实现 |
| `Snow.Http.Client` | HTTP 客户端: 基于 `IHttpClientFactory` 的命名客户端管线, 默认客户端可配基础地址 / 超时 / UserAgent / 请求头, 支持类型化客户端与泛型访问器 `ISnowHttpClient<T>` |
| `Snow.Caching` | 分布式缓存: 强类型包装 + `[Cacheable]` / `[CacheEvict]` 注解 |
| `Snow.Caching.StackExchangeRedis` | 缓存的 Redis 后端 |
| `Snow.DistributedLocking` | 分布式锁: 契约 + 进程内默认实现 + Redisson 式看门狗 + `[DistributedLock]` 注解 |
| `Snow.DistributedLocking.Redis` | 分布式锁的 Redis 后端(SET NX PX + Lua, 无第三方锁库) |
| `Snow.BackgroundJobs` | 后台任务: lambda 入队契约、任务记录、进程内存储与轮询 worker |
| `Snow.BackgroundJobs.Hangfire` | 后台任务的 Hangfire 适配: 接管入队与调度两个端口, 套上兼容级别与序列化器这些样板, 并提供 `UseSnowHangfireDashboard()` 供宿主在管道里挂仪表盘; 存储由宿主用 Hangfire 的 `AddHangfire` 自己指定 |
| `Snow.Excel` | Excel 导入导出契约: `IExcelService`、列映射注解 `[ExcelColumn]` / `[ExcelIgnore]`、导入导出选项, 含静默兜底实现(调用即报错) |
| `Snow.Excel.ClosedXML` | Excel 的 ClosedXML 实现: 对象集合或 `DataTable` 导出 xlsx(表头加粗/冻结/筛选/列宽), 按表头导回强类型列表, 提供原生 `XLWorkbook` 入口 |
| `Snow.Workflows` | 工作流契约: `ISnowWorkflowRunner`(按定义标识或类型启动)、结果快照 `SnowWorkflowResult`, 含静默兜底实现(调用即报错) |
| `Snow.Workflows.Elsa` | 工作流的 Elsa 3 实现: 接入 Elsa 引擎(内存或 EF Core / SQLite 持久化)、HTTP 端点与管理 API(宿主在管道里调 `UseSnowWorkflows()` 挂载, `EnableHttp` 控制), 顶替契约包的静默实现 |
| `Snow.Json` | JSON 序列化: 契约 `ISnowJsonSerializer` + 公共选项(`WriteIndented` / `IgnoreNullValues` / `AllowComments`) + **默认的 System.Text.Json 实现**(小驼峰、枚举转小驼峰字符串、忽略 null, 零第三方依赖), 对应 ABP 的 `Volo.Abp.Json` + `Volo.Abp.Json.SystemTextJson` |
| `Snow.Json.Newtonsoft` | JSON 的 Newtonsoft.Json 实现: 同样的驼峰与空值策略, 需要 Newtonsoft 行为(如 `$type` 保留)时装这个 |
| `Snow.ObjectMapping` | 对象映射契约: `IObjectMapper` 五个 Map 重载 + `source.Map<T>()` 链式扩展, 含静默兜底实现 |
| `Snow.ObjectMapping.Mapster` | 对象映射的 Mapster 实现(对应 ABP AutoMapper): `SnowMapsterProfile` 基类、`[SnowAutoMap]` 声明式映射、自动扫描模块程序集、`DefaultMaxDepth = 64`、启动即 `Compile()` 校验、可注入 `MapsterMapper.IMapper` |
| `Snow.Localization` | 本地化: 资源定义 + `IStringLocalizer` / `IStringLocalizerFactory` 实现, 按当前文化逐级回退 |
| `Snow.Settings` | 配置项系统(对应 ABP 的 Volo.Abp.Settings): 定义 `SettingDefinition` / 提供器 `SettingDefinitionProvider`、取值来源 `ISettingValueProvider`(默认读 `Settings:` 前缀配置节)、加密项与 `ISettingProvider` 门面 |

### 安全与其它

| 包 | 用途 |
| --- | --- |
| `Snow.Aop.Castle` | AOP 实现: 基于 Castle DynamicProxy 的接口代理与真异步方法拦截 |
| `Snow.Security` | 当前用户抽象与基于 `ClaimsPrincipal` 的默认实现; 文本加解密 `IStringEncryptionService`(AES/CBC + PBKDF2, 与 ABP 同算法) |
| `Snow.Authentication` | 认证: 一个模块 `SnowAuthenticationModule`(包内分 `Abstractions/` 与 `AspNetCore/` 两个目录, 同包同命名空间), 处理器是框架自带的 `JwtBearerHandler`。只有 `SecretKey` 必填(`Issuer` / `Audience` / `Expiration` 都有默认值), HS256 签发令牌、会话白名单、滑动续期(一直在用就一直在线)、踢人封禁在线数、注解鉴权、`TokenUtil` 门面 |
| `Snow.Authorization` | 权限: 定义树(自动扫描)、声明式校验器与动态策略(不存授予表), 策略名即权限名, `[Permission("Snow.Orders.Create")]` 直用 |
| `Snow.Emailing` | 邮件发送契约与配置项: 无状态 `IEmailSender`(`SendAsync` / `SendManyAsync`, 一封邮件一个 `EmailMessage`: 收件人/抄送/密送/回复地址/附件/优先级/双正文), 9 个 `Snow.Mailing.*` 配置项(密码加密)经 `ISettingProvider` 读取, 默认静默实现不真发 |
| `Snow.MultiTenancy` | 多租户内核: 租户上下文与切换、租户配置与存储(默认从 `MultiTenancy:Tenants` 读)、解析出资者, 默认关闭 |
| `Snow.Emailing.MailKit` | 邮件的 MailKit 实现: MimeKit 组装正文/附件, SMTP 走 SSL 或 STARTTLS 真投递(参数现读 `Snow.Mailing.*` 配置项); 批量发信复用同一条连接, 底层异常统一包成 `EmailSendException` |
| `Snow.Ftping` | FTP 客户端契约 + 配置项(`FtpSettingNames` / `FtpSettingProvider` / `IFtpConfiguration`, 无 `FtpOptions`), 含静默兜底实现 `NullFtpClient`(写操作记 warning 并返 false), 对应 ABP 的 FTP 抽象 |
| `Snow.Ftping.FluentFTP` | FTP 的 FluentFTP 实现: 每次调用建连, 上传/下载/删除/建目录/取目录列表; 连接参数从 `IFtpConfiguration` 现读(`BuildClientAsync()` 也暴露给宿主自管连接) |
| `Snow.Validation` | 参数校验: `ISnowObjectValidator` 做 DataAnnotations 校验(含嵌套对象), `[SnowValidation]` 方法级拦截, 统一抛 `SnowValidationException` |
| `Snow.Validation.FluentValidation` | 校验的 FluentValidation 后端: 自动扫描 `AbstractValidator<T>`, 把 DataAnnotations 与 FluentValidation 合成一条管线 |
| `Snow.ExceptionHandling` | 异常 → 错误信封: `IExceptionToErrorInfoConverter` / `DefaultExceptionToErrorInfoConverter` 把异常翻译成 `RemoteServiceErrorInfo`(错误码 / 消息 / 字段错误 / 附加数据), 由 `Snow.AspNetCore` 的 `UseSnowExceptionHandling()` 中间件写回 JSON; 自带 `zh-Hans` / `zh-Hant` / `en` 三份本地化文案(嵌入 JSON 经虚拟文件系统加载)。对应 ABP 的 `Volo.Abp.ExceptionHandling` |

## 注解一览

| 注解 | 作用 |
| --- | --- |
| `[Cacheable("Acme.User", Key = "id", ExpirationSeconds = 60)]` | 方法结果缓存: 第一个参数是缓存区名(同 Spring 的 value), Key / Condition / Unless 用表达式求值 |
| `[CacheEvict("Acme.User", Key = "id")]` | 方法执行后失效缓存; `AllEntries = true` 时整区失效 |
| `[DistributedLock("'stock:' + id", WaitSeconds = 3)]` | 方法级分布式锁, 支持 Prefix / Condition / LeaseSeconds / ThrowOnLost |
| `[Transactional]` | 方法级工作单元与事务, 支持 RequiresNew |
| `[Permission("Snow.Orders.Create")]` | 要求当前用户拥有该权限(策略名即权限名, 动态生成并缓存); 可写多个权限, 默认满足其一, `Mode = PermissionMatchMode.All` 改成都要有; 权限须先定义, 未定义时请求抛 500 |
| `[TokenCheckLogin]` / `[TokenCheckRole(...)]` | 登录 / 角色校验, 策略名 `snowauth:*` 动态生成; 权限直接写 `[Authorize("权限名")]` |
| `app.UseSnowExceptionHandling()` | 把任意异常翻译成统一的 JSON 错误信封(错误码 / 消息 / 字段校验错误), 挂在管道最外层; `SnowExceptionHandlingOptions` 的 `SendExceptionsDetailsToClients` / `SendStackTraceToClients` 控制是否暴露细节(选项不读配置节, 宿主在 `PreConfigureServices` 或任意模块里 `Configure<SnowExceptionHandlingOptions>(...)` 给出) |
| `app.UseSnowTokenExceptionHandling()` | 把登录态异常翻译成 401/403(宿主在管道里挂在 `UseAuthentication()` 之前, 这样认证阶段抛的令牌异常也兜得住; MVC 侧另有模块自动注册的 `TokenExceptionFilter` 接管 Controller 里的同类异常) |

## 许可

Apache License 2.0
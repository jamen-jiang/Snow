# Snow Framework

Snow 是一套 .NET 11 的模块化应用框架, 对标 ABP Framework: 模块系统、约定式依赖注入、AOP 拦截、
事件总线、命令/查询派发、工作单元与事务、EF Core 仓储、表达式引擎、分布式缓存与分布式锁。

所有包同版本发布, 版本号由仓库根目录的 `common.props` 统一维护。

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

// 加载启动模块及其全部依赖, 并立即执行服务注册阶段
builder.Services.AddApplication<MyModule>();

WebApplication app = builder.Build();

// 填好 Web 对象访问器, 再驱动各模块的初始化阶段
await app.InitializeSnowAsync();

app.Run();
```

模块类继承 `SnowModule` 并用 `[DependsOn]` 声明依赖:

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

## Web 宿主: 模块只做接线

`Snow.AspNetCore` 只有一件事: **Web 接线**(职责对位 ABP 的 `AbpAspNetCoreModule`)。它只引用接线本身跑得起来的 8 个基础模块 —— 审计 / 安全 / 授权 / 工作单元 / 数据过滤 / AOP / 时间 / 链路标识。
除此之外的能力都不在本包里: 应用层、认证、缓存、分布式锁、后台作业、事件总线、消息、邮件、FTP、HTTP 客户端、Excel、工作流、EntityFrameworkCore 以及它们的第三方实现, 由宿主按需引用。ABP 也是这个做法: `Volo.Abp.AspNetCore` 只依赖 9 个基础模块, Caching / EventBus / BackgroundJobs 全部留给宿主声明。
所以本包只有 10 个包引用, 没有一个是第三方依赖。

**包引用不等于模块加载。** `SnowAspNetCoreModule` 只自动装配它依赖的 8 个基础模块。缓存、分布式锁、后台作业、事件总线、消息、邮件、FTP、HTTP 客户端这些要自己引用包并声明依赖:

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
| 静态资源合并 | `AddSnowStaticAssets(...)` 登记的 `IFileProvider` 在启动时并进 `WebRootFileProvider`, 包里带的 wwwroot 可直接被访问 |

接线两步: 启动模块声明 `[DependsOn(typeof(SnowAspNetCoreModule))]`, 宿主里用 `await app.InitializeSnowAsync()` 代替 `app.Services.Initialize()`
(它先填访问器再驱动初始化, 顺序反了模块在初始化阶段拿到的访问器就是空的)。

```csharp
// 单例里取当前应用对象
public sealed class MyService(IObjectAccessor<IApplicationBuilder> accessor)
{
    public void MapSomething() => accessor.Value?.UseRouting();
}
```

```csharp
// 包内静态资源: 把程序集里的 wwwroot 暴露成 WebRoot 的一部分
builder.Services.AddSnowStaticAssets(
    new ManifestEmbeddedFileProvider(typeof(MyModule).Assembly, "wwwroot"));
```

配置节 `AspNetCore` 只有一项 `MergeStaticAssets`(默认 `true`), 置 `false` 可关掉合并。

### 端点统一挂载

映射端点要的是 `IEndpointRouteBuilder`, 而模块的应用初始化阶段只拿得到 `IApplicationBuilder` —— 所以模块不自己 `Map`, 而是把映射动作登记进管道登记表 `SnowPipelineRegistry`, 由 `InitializeSnowAsync()` 统一执行:

```csharp
WebApplication app = builder.Build();
await app.InitializeSnowAsync();   // 管道与端点都在这里挂完
app.Run();
```

`SnowMvcModule` 登记 `MapControllers()`, `SnowSignalRModule` 登记 Hub 映射, `SnowHealthChecksModule` 登记 `MapHealthChecks`, `SnowOpenApiModule` 登记文档端点 —— 宿主一行都不用写。
这与 ABP 的 `AbpEndpointRouterOptions.EndpointConfigureActions` 是同一套机制(差别只在于 ABP 把执行那一步留给宿主的 `UseConfiguredEndpoints()`)。

需要临时加端点:

```csharp
await app.InitializeSnowAsync();
app.UseSnowEndpoints(endpoints => endpoints.MapGet("/ping", () => "pong"));   // 已挂过, 只追加这一段
```

只在单独使用某个包、且不经过 `InitializeSnowAsync()` 时, 才需要手写 `app.UseSnowControllers()` / `app.MapSnowHubs()` / `app.MapSnowHealthChecks()`。

### 链路标识

每个请求一个链路标识: 先看 `X-Correlation-Id` 请求头, 没有就生成一个, 并写回响应头。请求内 `ICorrelationIdProvider.Get()` 返回同一个值, 日志增强、审计、出站 HTTP 都用它把跨服务日志串起来。

中间件由 `SnowAspNetCoreModule` 登记到 `BeforeRouting` 阶段, 宿主不用写任何一行:

```csharp
await app.InitializeSnowAsync();   // 链路标识已挂到管道最外层
```

配置节 `CorrelationId`: `{ "HttpHeaderName": "X-Correlation-Id", "SetResponseHeader": true }`。

它与 ABP 的 `app.UseCorrelationId()` / `AbpCorrelationIdMiddleware` 同形, 由 `Snow.AspNetCore` 提供 —— **不放在 Serilog 包里**: Serilog 只消费这个值, 而审计、出站 HTTP、事件总线也都要用它, 放进去会逼着不用 Serilog 的宿主也引 Serilog(ABP 同样把它放在 `Volo.Abp.AspNetCore`)。

### 中间件管道

宿主一行都不用写: 模块在 `ConfigureServices` 里用一行 `AddSnowMiddleware` 声明自己的中间件属于哪个阶段, `InitializeSnowAsync()` 按阶段顺序统一展开。

```csharp
WebApplication app = builder.Build();
await app.InitializeSnowAsync();   // 全部中间件 + 全部端点在这里挂完
app.Run();
```

为什么需要这一层: ASP.NET Core 的管道是按代码顺序拼出来的链表, 没有插入 API; 而模块的 `OnApplicationInitialization` 是按模块依赖顺序依次执行的, 表达不了"认证之后、授权之前"这种夹在中间的位置。所以位置用阶段表达, 阶段之间的路由 / 认证 / 授权三段由 `SnowAspNetCoreModule` 自己插进去:

| 阶段 | 位置 | 谁登记 |
| --- | --- | --- |
| `Outermost` | 一切之前 | `Snow.Authentication`(登录态异常转 401/403, 兜住后面所有中间件) |
| `BeforeRouting` | 路由之前 | `SnowAspNetCoreModule`(链路标识)、`Snow.AspNetCore.Serilog`(日志上下文增强) |
| — | `UseRouting()` | 框架插入; 没注册路由服务时自动跳过 |
| `AfterRouting` | 路由之后、认证之前 | 需要读端点/路由值但不需要用户的中间件 |
| — | `UseAuthentication()` | 框架插入; 没有任何认证方案时自动跳过 |
| `AfterAuthentication` | 认证之后、授权之前 | `Snow.AspNetCore.MultiTenancy`(要读已认证用户与路由值) |
| — | `UseAuthorization()` | 框架插入; 没有授权服务时自动跳过 |
| `AfterAuthorization` | 授权之后、端点之前 | `Snow.AspNetCore.RateLimiting`(要读端点上的限流策略) |
| `BeforeEndpoints` | 端点之前的终端分支 | `Snow.BackgroundJobs.Hangfire`(仪表盘, 受 `EnableDashboard` 控制) |

模块侧就一行:

```csharp
public override void ConfigureServices(ServiceConfigurationContext context)
{
    // 中间件类型本身就是去重键: 同一类型登记两次会在启动期直接报错, 不会静默生效
    context.Services.AddSnowMiddleware<MultiTenancyMiddleware>(SnowPipelineStage.AfterAuthentication);
}
```

微软自家中间件是 `internal` 的(如限流只能走 `UseRateLimiter()`), 这种用重载:

```csharp
context.Services.AddSnowMiddleware(SnowPipelineStage.AfterAuthorization, app => app.UseRateLimiter());
```

宿主自己加中间件, 按需要选:

```csharp
// 1) 不关心位置: 原生写法, 放在 InitializeSnowAsync 前后即可
app.UseMyOuterMiddleware();
await app.InitializeSnowAsync();     // 之前 = 整段之前, 之后 = 整段之后
app.UseMyInnerMiddleware();

// 2) 关心位置: 和包内中间件同一套写法, 只写阶段, 不写任何序号
builder.Services.AddSnowMiddleware<MyMiddleware>(SnowPipelineStage.AfterAuthorization);

// 3) 内联: 需要临时插一段逻辑
builder.Services.AddSnowMiddleware(SnowPipelineStage.AfterRouting, app => app.Use(async (context, next) =>
{
    // ...
    await next(context);
}));
```

- 中间件实现本身仍是原生写法: `app.UseMiddleware<T>()` 或实现 `IMiddleware`; 登记只决定"挂在哪个阶段", 不改变"怎么挂"。
- 同一阶段内按登记顺序执行; 同一中间件类型重复登记直接抛 `SnowInitializationException`。
- 注意: 约定式中间件构造器注入的 Scoped 服务实际只建一次(启动期); 需要每请求作用域实例就继承 `SnowMiddlewareBase`(实现 `IMiddleware`, 容器按请求构造), 或用 `InvokeAsync` 参数注入。
- 与 Spring 对照: Spring 是 `Filter` 集合 + `@Order` 数字全排序, 库能自我装配, 代价是顺序成了公共契约、每个 filter 都要维护一个数字; 这里换成阶段枚举, 数量固定、语义自解释, 库照样自我装配。


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

配置走 `BackgroundJobs` 配置节, 执行开关与轮询参数都在同一个类里:

| 配置 | 默认 | 作用 |
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

// Program.cs: 仪表盘由模块按 Hangfire 配置节自动挂好(默认 /hangfire), 宿主不用写 UseSnowHangfireDashboard()
await app.InitializeSnowAsync();
```

模块做的事: 关掉内置 worker(免得它空轮询进程内存储), 并接上 Hangfire 的服务端与存储; 两个端口本身的替换不写代码 —— `HangfireBackgroundJobManager` / `HangfireBackgroundJobScheduler` 上标了 `ReplaceServices = true`, 约定注册会把默认实现顶掉(与 MailKit / FTP / Excel 那批同一套路)。
配置节 `Hangfire`: `EnableServer` / `EnableDashboard` / `DashboardPath` / `DashboardTitle` / `AppPath` / `UseInMemoryStorage` / `WorkerCount` / `PollingIntervalSeconds` / `TimeZoneId`。

Hangfire 侧入队直接把 lambda 交给 `Job.FromExpression`, 仪表盘里显示的是真实的服务与方法
(`IMailService.SendAsync`), 不是一个统一的转发器。参数在入队时求值并由 Hangfire 序列化。

两种后端共用同一份契约与同一条调用约定, 所以同一段入队代码在进程内 worker 与 Hangfire 上行为一致。

## 认证: 一个模块, 一项必填

`Snow.Authentication` 只有一个模块 `SnowAuthenticationModule`, 方案名 `SnowAuthentication`, 处理器是框架自带的
`JwtBearerHandler` —— 验签、拼 `WWW-Authenticate` 都由框架完成, 本包只补它没有的两件事:
用 HS256 签发令牌, 以及验签通过后再查一次服务端会话(白名单)。

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
[RequirePermission("Snow.Orders.Create")]          // 权限, 走 Snow.Authorization 的权限定义与校验
[TokenCheckRole("admin", "ops")]                   // 角色, 具备任一即通过
```

登录与角色注解只是把策略名写成 `snowauth:login` / `snowauth:role:...`, 由 `TokenPolicyProvider` 动态翻译成要求。
权限注解用 `Snow.Authorization` 的 `[RequirePermission]`(策略名前缀 `permission:`), 其余策略名原样交给
`SnowAuthorizationPolicyProvider`, 两套写法共用同一份策略缓存。

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

响应体是 `ProblemDetails`(`application/problem+json`)。MVC 宿主由模块自动注册的 `TokenExceptionFilter` 接管;
最小 API 宿主也不用写任何一行 —— 模块已把 `TokenExceptionMiddleware` 登记到 `Outermost` 阶段, `InitializeSnowAsync()` 时自动挂上。
只拦上面三类, 其它异常原样抛出, 不影响宿主自己的错误处理。


## 权限: 定义一次, 注解即用

`Snow.Authorization` 管"能干什么", 认证(谁是你)由 `Snow.Authentication` 负责。

### 1. 定义权限

继承 `PermissionDefinitionProvider`, 只覆写 `Define`:

```csharp
public sealed class OrdersPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
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
`IPermissionDefinitionProvider` / `IPermissionValueProvider` 实现自动登记。只要你的模块程序集进了
`[DependsOn]` 链, 类型就在扫描范围内。

### 2. 注解鉴权

```csharp
[RequirePermission("Snow.Orders.Create")]
public Task CreateOrderAsync(...) { ... }
```

`[RequirePermission]` 把策略名写成 `permission:Snow.Orders.Create`, 由 `SnowAuthorizationPolicyProvider`
动态翻译成"要求已登录 + 拥有该权限", 不用为每个权限写 `AddPolicy`。策略对象构造一次后缓存。

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

批量判断用 `IsGrantedAsync(names)`, 返回 `MultiplePermissionGrantResult`(每项三态: 授予 / 未定 / 禁止)。

### 4. 授予与回收

```csharp
// 给角色 admin 授予
await permissionManager.SetAsync("Snow.Orders.Create", "R", "admin", isGranted: true);

// 给用户 42 回收
await permissionManager.SetAsync("Snow.Orders.Create", "U", "42", isGranted: false);
```

默认仓储是进程内实现, 重启即丢。接数据库只需写一个 `IPermissionGrantRepository` 实现并标
`[SingletonService(ReplaceServices = true)]`, 校验链路一行都不用改。

### 判定规则

两个内置值提供器按顺序求值, 三态聚合:

| 名 | 判据 | 说明 |
| --- | --- | --- |
| `U` | 按 `UserId` 查授予表 | 运行期可改 |
| `R` | 按全部角色查授予表 | 任一角色被授予即通过 |

规则: 任一提供器返回 **授予** 且没有提供器返回 **禁止** 才通过; **禁止** 一票否决。
权限对象上的 `Providers` 列表可限定只让指定提供器参与求值(为空表示不限制)。

自定义提供器同样只需实现 `IPermissionValueProvider`(或继承 `PermissionValueProvider`), 会自动扫描登记,
内置两个排在前面。

## HTTP 客户端

`Snow.Http.Client` 把 ASP.NET Core 的 `IHttpClientFactory` 接进来, 对外只有两种用法: **声明式接口**(对应 Spring 的 `@HttpExchange` 接口, 推荐) 和 **低层访问器**(对应注入一个配好的 `RestClient`, 用于流、非 JSON 与一次性调用)。

```csharp
[DependsOn(typeof(SnowHttpClientModule))]
public class MyModule : SnowModule { }
```

```json
{
  "HttpClient": {
    "BaseAddress": "https://api.example.com",
    "Timeout": "00:00:30",
    "UserAgent": "Snow/1.0",
    "DefaultHeaders": { "X-Tenant": "acme" }
  }
}
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

名字由契约类型推导: 去掉前导 `I` 与尾部 `Client`(`IHkoTrackingClient` 得到 `HkoTracking`, `IMDDB65127Client` 得到 `MDDB65127`), 配置节写成 `HttpClient:{名字}`, 没写就回退 `HttpClient` 顶层的默认值:

```json
{
  "HttpClient": {
    "Timeout": "00:00:05",
    "UserAgent": "Snow/1.0",
    "HkoTracking": { "BaseAddress": "https://hko.example.com" },
    "MDDB65127": { "BaseAddress": "https://mddb.example.com", "Timeout": "00:00:30" }
  }
}
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

```csharp
[DependsOn(typeof(SnowElsaModule))]
public class MyModule : SnowModule
{
    // PreConfigureServices 在所有模块注册服务之前执行, AddSnowElsa 的设置才能被 Elsa 模块读到
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddSnowElsa(options =>
        {
            options.WorkflowTypes.Add(typeof(OrderApprovalWorkflow));   // 代码定义的工作流
        });
    }
}
```

配置节 `Workflows:Elsa`:

```json
{
  "Workflows": {
    "Elsa": {
      "Database": "Sqlite",                    // None(内存, 默认) | Sqlite
      "ConnectionString": "Data Source=elsa.db",
      "RunMigrations": true,                   // 由宿主启动阶段执行, 只在真正的 ASP.NET 宿主生效
      "EnableHttp": true,
      "HttpBasePath": "/workflows",
      "EnableApi": true
    }
  }
}
```

启动两种姿势:

```csharp
// 按定义标识(定义来自管理 API / 界面录入并已发布)
SnowWorkflowResult r1 = await runner.StartAsync("order-approval", new Dictionary<string, object?> { ["orderId"] = 42 });

// 按代码类型
SnowWorkflowResult r2 = await runner.StartAsync<OrderApprovalWorkflow>(input);

// 宿主管道里挂 Elsa 的 HTTP 端点
app.UseSnowWorkflows();
```

- 结果 `SnowWorkflowResult` 只透出字符串状态(`Status` / `SubStatus`)与 `Incidents`, 不把 Elsa 的枚举泄漏到业务层; `Succeeded` 表示状态与子状态都是 `Finished`。
- 定义找不到、类型不可执行、引擎未安装都抛 `SnowWorkflowException`。
- 换 SQL Server / PostgreSQL: 自己加对应的 `Elsa.Persistence.EFCore.*` 包, 再通过 `options.ConfigureWorkflowManagement` / `options.ConfigureWorkflowRuntime` 换提供程序(包内只带 SQLite)。
- 不引实现包也能启动, 但调用 `ISnowWorkflowRunner` 会立刻抛 `SnowWorkflowException` 并提示装 `Snow.Workflows.Elsa`。

## 审计

方法级审计只做两件事: 记下每次调用的调用方、耗时、参数与异常, 以及给实体填创建/修改/删除字段。

### 方法级审计

```csharp
[DependsOn(typeof(SnowAuditingModule))] // 通常随 Snow.AspNetCore 一起加载
public sealed class MyModule : SnowModule { }

[Auditing] // 标在类上对全部接口方法生效, 也可只标在某个方法上
public sealed class OrderAppService : IOrderAppService
{
    public Task CreateAsync(string name, int count) => ...;
}
```

审计日志默认不落任何介质: 内置的 `NullAuditingStore` 只写调试日志。要落库就替换掉它(约定注册会跳过已显式登记的服务类型, 所以 `TryAddSingleton` 有效):

```csharp
public sealed class MyAuditingStore : IAuditingStore
{
    public Task SaveAsync(AuditLogInfo auditLogInfo, CancellationToken cancellationToken = default)
    {
        // auditLogInfo.UserId / UserName / HttpMethod / Url / ExecutionDuration / Actions / Exceptions
        return Task.CompletedTask;
    }
}

public override void ConfigureServices(ServiceConfigurationContext context)
{
    context.Services.TryAddSingleton<IAuditingStore, MyAuditingStore>();
}
```

### 实体审计字段

`IAuditPropertySetter` 按实体实现的契约自动填值, 不需要手写赋值:

| 契约 | 填什么 |
| --- | --- |
| `IHasCreationTime` / `ICreationAudited` | `CreationTime`、`CreatorId`、`Creator` |
| `IHasModificationTime` / `IModificationAudited` | `LastModificationTime`、`LastModifierId`、`LastModifier` |
| `IDeletionAudited` / `ISoftDelete` | `DeletionTime`、`DeleterId`、`Deleter`、`IsDeleted = true` |

### 配置

```json
{
  "Auditing": {
    "IsEnabled": true,
    "IsEnabledForAnonymousUsers": true,
    "IsEnabledForGetRequests": false,
    "ApplicationName": "MyApp",
    "MaxSerializedLength": 32000
  }
}
```

`IgnoredNamespaces` 默认含 `Snow.`, 因此框架自身的类型不会被当成业务审计; 要连框架类型也记就设 `AlwaysLogFrameworkTypes = true`。保存日志失败只记错误日志, 不会把业务调用拖垮。

## MVC 与自动 API

`SnowMvcModule` 把应用服务按约定直接暴露成 Controller, 服务端不用写任何 Controller 类(对应 ABP 的 `AbpAspNetCoreMvcModule` + 自动 API)。整个机制三件事: 应用服务登记成 Controller(特性提供器)、路由与动词按约定算(应用模型约定)、结果与异常统一成信封(全局过滤器)。

```csharp
[DependsOn(typeof(SnowMvcModule))]
public sealed class MyHttpApiHostModule : SnowModule { }
```

```csharp
// 继承 ApplicationService 即可, 不需要 [ApiController], 也不需要继承 ControllerBase
public class BookAppService : ApplicationService
{
    public BookAppService(IServiceProvider serviceProvider) : base(serviceProvider) { }

    public Task<BookDto> GetAsync(Guid id) => ...;              // GET api/app/book/{id}
    public Task<List<BookDto>> GetListAsync() => ...;           // GET api/app/book
    public Task<BookDto> CreateAsync(CreateBookDto input) => ...; // POST api/app/book
}
```

### 路由与动词约定

| 环节 | 规则 |
| --- | --- |
| 控制器名 | 类名去掉后缀 `ApplicationService` / `AppService` / `Service` 后转 kebab-case(`BookAppService` → `book`) |
| 控制器路径 | `RootPath`(默认 `api/app`) + `/` + 控制器名, 即 `api/app/book` |
| GET 列表 | `GetList` / `GetAll` / `GetPaged` / `List` → `GET api/app/book` |
| GET 单条 | `Get` / `Find` / `Load`, 且有唯一的简单类型参数 `id` → `GET api/app/book/{id}`; 否则 → `GET api/app/book/{方法名 kebab}` |
| 新增 | `Create` / `Add` / `Insert` / `Post` → `POST api/app/book` |
| 修改 | `Update` / `Edit` / `Modify` / `Put` → `PUT`; `Patch` → `PATCH` |
| 删除 | `Delete` / `Remove` / `RemoveBy` → `DELETE` |
| 其余 | `POST api/app/book/{方法名 kebab}` |

参数绑定: 名为 `id` 的简单类型走路由; 复杂类型走请求体; 其余简单类型走查询串; `CancellationToken` 忽略。简单类型含 primitive、enum、string、decimal、Guid、各种日期时间类型、TimeSpan、Uri。

### 统一响应信封

默认把返回值包成 `SnowApiResponse`(对应 ABP 的 `RemoteServiceResponse`):

```json
{ "success": true, "result": { "id": "...", "title": "..." }, "error": null }
{ "success": false, "result": null,
  "error": { "code": "ValidationError", "message": "请求参数校验失败",
             "details": null, "validationErrors": { "title": ["不能为空"] } } }
```

单个类或方法要原样返回(不包装)加 `[SnowWrapResult(false)]`。异常按类型翻译:

| 异常 | 状态码 | 错误码 |
| --- | --- | --- |
| `SnowUserFriendlyException` | 400 | 异常自带的 `Code`, 未给则 `BusinessError` |
| `UnauthorizedAccessException` | 403 | `Forbidden` |
| `KeyNotFoundException` | 404 | `NotFound` |
| `NotImplementedException` | 501 | `NotImplemented` |
| `ArgumentException` / `InvalidOperationException` | 400 | `InvalidArgument` |
| 其余 | 500 | `InternalError`(消息固定"服务器内部错误", 除非打开 `IncludeExceptionMessage`) |

模型校验失败自动回 400 与字段错误列表(`ValidationError`), 等价于 `[ApiController]` 的默认行为。

### 接线与配置

```csharp
WebApplication app = builder.Build();
await app.InitializeSnowAsync();   // 管道 + 全部端点(Controller / Hub / 健康检查 / 文档)一次挂完
app.Run();
```

```json
{
  "Mvc": {
    "RootPath": "api/app",
    "AutoApiControllers": true,
    "WrapResultByDefault": true,
    "IncludeExceptionMessage": false,
    "AutoModelStateValidation": true
  }
}
```

选项也可在 `PreConfigureServices` 里用 `context.Services.AddSnowMvc(options => ...)` 调(`AddApplication` 之前)。

## OpenAPI 文档与调试界面

`SnowOpenApiModule` 用官方 `Microsoft.AspNetCore.OpenApi` 生成文档, 用 Scalar 提供调试界面(对位 ABP 的 `Volo.Abp.Swashbuckle`)。
自动带上全部自动 API 的路径, 可选加 Bearer 认证方案, 并自动把 XML 注释贴成接口说明。依赖 `SnowMvcModule`。

```csharp
[DependsOn(typeof(SnowOpenApiModule))]
public sealed class MyHttpApiHostModule : SnowModule { }
```

```csharp
await app.InitializeSnowAsync();   // 文档端点已由 SnowOpenApiModule 登记, 默认 /swagger/v1.json 与 /swagger 界面
```

单独使用本包(不走 `InitializeSnowAsync()`)时才需要手写 `app.UseSnowSwagger();`。

选项既可以走配置节 `Swagger`, 也可以在 `PreConfigureServices` 里用 `context.Services.AddSnowSwagger(options => ...)` 覆盖(后执行者赢):

```json
{
  "Swagger": {
    "Enabled": true,
    "Title": "Snow API",
    "Version": "v1",
    "Description": "订单与商品接口",
    "RoutePrefix": "swagger",
    "IncludeXmlComments": true,
    "AddJwtBearerSecurity": true
  }
}
```

`IncludeXmlComments` 打开时会自动找各模块程序集目录下的 `{程序集名}.xml`(生成项目需开 `GenerateDocumentationFile`), 把三行 summary 写成接口摘要、把 `<param>` 写成参数说明、把属性的 summary 写成字段说明。`AddJwtBearerSecurity` 在文档里加 `Bearer` 方案, 界面上可直接填令牌(只填令牌本身, 不加 `Bearer ` 前缀)调试。

文档名就是 `Version`, 所以对外地址是 `/{RoutePrefix}/{Version}.json`(默认 `/swagger/v1.json`)。

## 健康检查

```csharp
[DependsOn(typeof(SnowHealthChecksModule))]
public sealed class MyHttpApiHostModule : SnowModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddSnowHealthCheck<DbHealthCheck>("db", "ready");
    }
}
```

```csharp
await app.InitializeSnowAsync();   // 默认映射 GET /health, 由模块登记, 宿主不用写
```

单独使用本包时才需要手写 `app.MapSnowHealthChecks();`。

```json
{
  "HealthChecks": {
    "Path": "/health",
    "RequireAuthorization": false,
    "IncludeDetails": true
  }
}
```

响应是 JSON: 总体 `status`、总耗时 `totalDuration`, 以及每个检查项的 `name` / `status` / `duration` / `description` / `exception`。`RequireAuthorization = true` 时端点要求已认证。

## SignalR

```csharp
[SnowHub("/signalr/chat")]        // 路径可省, 省了按类名算: ChatHub → /signalr/chat
public sealed class ChatHub : Hub { }
```

```csharp
// 方式一: 手动登记
context.Services.AddSnowHub<ChatHub>();
// 方式二: 自动扫描全部模块程序集里的 Hub 派生类(默认开启)
context.Services.AddSnowSignalR(options => options.EnableDetailedErrors = true);
```

```csharp
await app.InitializeSnowAsync();   // Hub 端点由模块登记, 宿主不用写
```

单独使用本包时才需要手写 `app.MapSnowHubs();`。

```json
{
  "SignalR": {
    "Enabled": true,
    "AutoDiscoverHubs": true,
    "EnableDetailedErrors": false,
    "MaximumReceiveMessageSize": 32768
  }
}
```

## 限流

直接用微软自带的限流中间件(`Microsoft.AspNetCore.RateLimiting`), 本包只做两件事: 在模块里帮你调用 `AddRateLimiter`, 并把 `UseRateLimiter()` 登记到 `AfterAuthorization` 阶段。**没有任何自有选项类型**: 配置动作收到的就是原生的 `RateLimiterOptions`。

```csharp
[DependsOn(typeof(SnowRateLimitingModule))]
public sealed class MyHttpApiHostModule : SnowModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddSnowRateLimiting(limiter =>
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
    }
}
```

```csharp
await app.InitializeSnowAsync();   // 限流中间件已由 SnowRateLimitingModule 登记(order 700), 宿主不用写
app.Run();
```

单独使用本包时才需要手写 `app.UseSnowRateLimiting();`(内部就是 `app.UseRateLimiter()`)。

说明:

- `AddSnowRateLimiting(...)` 必须在 `PreConfigureServices`(或 `AddApplication` 之前)调用, 动作只是被收集, 真正下发在模块的 `ConfigureServices` 里。
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

// 宿主模块 OnApplicationInitialization, 顺序与 ABP 模板一致
app.UseRouting();
app.UseCors();
app.UseAuthentication();
```

`configuration` 就是 `context.Configuration`(或 `context.Services.GetConfiguration()`)。需要额外暴露响应头时在 `AllowCredentials()` 之前加 `.WithExposedHeaders("你的头")` —— 对应 ABP 的 `WithAbpExposedHeaders()`。

## 框架服务: 设置 / 功能 / 校验 / 异常 / 对象存储 / 播种

这批包的关系是"契约 + 默认实现", 引进来就可用, 要换后端再加实现包或改配置。

| 需求 | 用法 |
| --- | --- |
| 设置 | 继承 `SettingDefinitionProvider` 定义, `ISettingProvider.GetAsync<T>("MyApp.Mail.Host")` 读取; 值来源优先级 租户(T) > 全局存储(G) > 配置(C, `Settings:{名}`) > 默认值(D), 顺序由 `SnowSettingOptions.ValueProviders` 决定(列表靠后优先), 与 ABP 的 `AbpSettingOptions` 一致 |
| 功能开关 | 继承 `FeatureDefinitionProvider` 定义, `IFeatureChecker.IsEnabledAsync("MyApp.Feature")` 判定; 值来源 配置(C, `Features:{名}`) > 默认值(D) |
| 参数校验 | `[SnowValidation]` 标在应用服务或方法上, 复杂参数按 DataAnnotations 校验, 有错抛 `SnowValidationException`; 再加 `Snow.Validation.FluentValidation` 则同一处同时跑 FluentValidation 规则 |
| 对象映射 | 注入 `IObjectMapper`(`Map<OrderDto>(order)` / `Map(order, dto)`); `Snow.ObjectMapping.Mapster` = ABP AutoMapper 的 Mapster 版: `SnowMapsterProfile` + `[SnowAutoMap]` + `AddProfile`/`AddMaps` + `DefaultMaxDepth`/`ValidateMappings` |
| 异常翻译 | `app.Services.ToErrorResponse(exception)` 得到统一错误对象; 业务异常用 `SnowUserFriendlyException` 可带 `Code` 与附加数据 |
| 对象存储 | 注入 `IBlobContainer<MyAvatarBlobContainer>`, 容器名由类型名去掉 `BlobContainer` 后缀得到, 或用 `[BlobContainerName("avatars")]` 指定; 换后端在 `PreConfigureServices` 里 `services.AddSnowBlobStoring<MyBlobProvider>()`(或 `AddSnowBlobStoring(o => o.ProviderType = ...)`), 写在同一处配置节里与代码动作冲突时以代码动作为准 |
| 数据播种 | 实现 `IDataSeedContributor`, 启动完成时按 `DataSeeding:SeedOnStartup` 自动执行 |
| 本地化 | `[LocalizationResource("MyApp")]` 标资源类, 注入 `IStringLocalizer<MyAppResource>` 取字符串 |
| JSON | 注入 `ISnowJsonSerializer`, 选项与框架其余部分一致, 不要在业务里另建 `JsonSerializerOptions`; `Snow.Json` 自带 System.Text.Json 实现, 要 Newtonsoft 行为再加 `Snow.Json.Newtonsoft` |
| 虚拟文件系统 | `AddSnowVirtualFileSystem(files => files.AddEmbedded<MyModule>("assets", "MyAssets"))`, 注入 `IVirtualFileProvider` |

开关与值的层级用配置节控制, 例如:

```json
{
  "Settings": { "MyApp.Mail.Host": "smtp.example.com" },
  "Features": { "MyApp.NewCheckout": true },
  "DataSeeding": { "SeedOnStartup": false },
  "BlobStoring": { "ProviderType": "MyApp.MyBlobProvider, MyApp" }
}
```

`BlobStoring:ProviderType` 必须写成 `"命名空间.类型, 程序集名"`(例如 `"MyApp.MyBlobProvider, MyApp"`), 只写 `"MyApp.MyBlobProvider"` 解析不到就启动报错。换成内存实现以外的后端两种写法等价: 配置节, 或 `PreConfigureServices` 里 `services.AddSnowBlobStoring<MyBlobProvider>()`; 两处同时写时代码动作生效。

## 多租户

两个包, 与 ABP 的 `Volo.Abp.MultiTenancy` / `Volo.Abp.AspNetCore.MultiTenancy` 一一对应:

- `Snow.MultiTenancy` —— 内核, 与环境无关: 当前租户上下文、租户配置、租户存储、解析出资者、租户级设置
- `Snow.AspNetCore.MultiTenancy` —— Web 适配层: 四个请求侧出资者 + 中间件

默认**关闭**(与 ABP 一致): 不开的时候 `ICurrentTenant.Id` 恒为 `null`, 中间件原样放行, 代码照常跑。

### 打开与取用

```csharp
[DependsOn(typeof(SnowAspNetCoreMultiTenancyModule))]
public sealed class MyHttpApiHostModule : SnowModule;

// Program.cs
await app.InitializeSnowAsync();   // 路由 / 认证 / 多租户 / 授权 / 端点全在这一次里按序挂好
app.Run();
```

```json
{
  "MultiTenancy": {
    "IsEnabled": true,
    "Tenants": [
      { "Id": "11111111-1111-1111-1111-111111111111", "Name": "Acme" },
      { "Id": "22222222-2222-2222-2222-222222222222", "Name": "Beta" },
      { "Id": "33333333-3333-3333-3333-333333333333", "Name": "Old", "IsActive": false }
    ],
    "AspNetCore": { "TenantKey": "__tenant" }
  }
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

中间件由 `SnowAspNetCoreMultiTenancyModule` 登记到 `AfterAuthentication` 阶段自动挂载(要读路由值, 也要读已认证用户), 宿主不用写 `app.UseSnowMultiTenancy()`。

### 租户存储

- 默认 `DefaultTenantStore`: 从 `MultiTenancy:Tenants` 读, 名称比较去掉首尾空白且忽略大小写, `IsActive: false` 的租户当作不存在
- 换数据库实现: `context.Services.Replace(ServiceDescriptor.Singleton<ITenantStore, MyTenantStore>())`; 契约是 `FindAsync` / `GetAsync` 各两个重载
- 自己解析并翻译: `ITenantConfigurationProvider.GetAsync(saveResolveResult: true)` 一步拿到 `TenantConfiguration`, 结果同时写进 `ITenantResolveResultAccessor`

### 租户级设置

`TenantSettingValueProvider`(名字 `T`)自动接进设置链: 在租户上下文里读设置, 会先问租户那一层, 取不到才落到全局存储 `G` / 配置 `C` / 默认值 `D`。存储实现要按三元组落库:

```csharp
public interface ISettingStore
{
    Task<string?> GetOrNullAsync(string name, string? providerName = null, string? providerKey = null, CancellationToken cancellationToken = default);
}
// 全局设置: ("MyApp.Mail.Host", "G", null)
// 租户设置: ("MyApp.Mail.Host", "T", tenantId.ToString())
```

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
| `options.AddProfile<T>()` / `AddMaps<TModule>()` | `AddProfile<TProfile>()` / `AddMaps<TModule>()` / `AddMaps(params Assembly[])` |
| `Configurators` / `DefaultMaxDepth = 64` / `ValidatingProfiles` + `AssertConfigurationIsValid` | `Configurators` / `DefaultMaxDepth = 64` / `ValidateMappings = true` |
| 注入 AutoMapper 的 `IMapper` | 注入 `MapsterMapper.IMapper` |

```csharp
[DependsOn(typeof(SnowObjectMappingMapsterModule))]
public sealed class MyModule : SnowModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddSnowObjectMapping(mapster =>
        {
            mapster.AddProfile<OrderMappingProfile>();  // 或 mapster.AddMaps<MyModule>() 扫本节程序集内的全部 Profile
            mapster.DefaultMaxDepth = 64;
            mapster.ValidateMappings = true;            // 启动即编译: 映射写错, 应用起不来
        });
    }
}

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
```

- 注入 `IObjectMapper`: `Map<OrderDto>(order)` 映射成新对象, `Map(order, dto)` 就地填充已有对象, `Map(source, typeof(Order), typeof(OrderDto))` 走运行时类型
- 配置节 `ObjectMapping:Mapster`: `Enabled` / `CompileMappings`(启动即编译)/ `ValidateMappings`(启动即编译, 失败包成 `SnowObjectMappingException`)/ `ScanRegisters` / `ScanAutoMaps` / `DefaultMaxDepth` / `MapAssemblies`
- 实现 Mapster 的 `IRegister` 依旧自动扫描; 需要碰底层 `TypeAdapterConfig` 时用 `mapster.Configure = config => ...` 或 `mapster.Configurators.Add(...)`
- 不引实现包时 `IObjectMapper` 是 `NullObjectMapper`, 调用即抛"请引入 Snow.ObjectMapping.Mapster 包"

### 校验(FluentValidation)

- `Snow.Validation`: DataAnnotations 校验 + `[SnowValidation]` 方法级拦截
- `Snow.Validation.FluentValidation`(对应 ABP 的 Volo.Abp.FluentValidation): 实现 `AbstractValidator<T>` 即自动登记, 规则与 ABP 的 `AbpFluentValidationConventionalRegistrar` 一致 —— 瞬态生命周期, 同时暴露具体校验器类型与 `IValidator<T>`(可以直接注入 `CreateOrderDtoValidator`), 标了 `[DisableConventionalRegistration]` 的跳过, 也可用 `AddSnowFluentValidation(o => { o.ScanValidators = false; o.ValidatorTypes.Add(typeof(XxxValidator)); })` 只点名登记; 并把 `ISnowObjectValidator` 换成"先 DataAnnotations 再 FluentValidation, 错误合成一份"的管线
- MVC 侧的自动校验不另建包: `SnowObjectValidationActionFilter` 就在 `Snow.AspNetCore.Mvc` 里(对应 ABP 把 `AbpValidationActionFilter` 放在 Volo.Abp.AspNetCore.Mvc)。它用 `ISnowObjectValidator` 校验 Controller(含自动 API)的复杂参数, 不通过直接返回 400 + `SnowApiResponse.Fail("ValidationError", ...)`, 过滤器顺序 -1000(最前), 开关是 `Mvc:AutoModelStateValidation`。因此换成 FluentValidation 后 MVC 侧行为自动跟着变, 不需要再加任何包。

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

配置节: `Validation:FluentValidation`(`Enabled` / `ScanValidators` / `ValidateDataAnnotations`)。MVC 自动校验的开关在 `Mvc:AutoModelStateValidation`。

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

配置节: `Json`(`WriteIndented` / `IgnoreNullValues` / `AllowComments`)、`Json:SystemTextJson`(库级选项, 代码里也可用 `services.AddSnowSystemTextJson(o => ...)` 增删转换器)、`Json:Newtonsoft`(Newtonsoft 的 `JsonSerializerSettings`)。

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
| `Snow.Core` | 内核: 模块系统、约定式依赖注入、表达式求值、System 扩展方法 |
| `Snow.Aop` | AOP 契约: 方法调用上下文、拦截器特性、代理工厂接口(实现见 `Snow.Aop.Castle`) |
| `Snow.Data` | 数据过滤: 按过滤器类型开关全局过滤(软删除等), 状态按异步调用链隔离 |
| `Snow.Timing` | 时间: `IClock` 抽象与直接读系统时钟的默认实现 |
| `Snow.Tracing` | 链路标识: `ICorrelationIdProvider` 与进程内默认实现 |
| `Snow.AspNetCore` | Web 宿主能力包: `SnowAspNetCoreModule`(HTTP 上下文访问器、Web 对象访问器、包内静态资源合并、按阶段展开管道与端点) + `UseSnowAspNetCore()` / `InitializeSnowAsync()`, 管道登记表 `SnowPipelineRegistry` + 阶段枚举 `SnowPipelineStage`(`AddSnowMiddleware<T>(stage)` 声明中间件、`AddSnowEndpoint(...)` 声明端点, 初始化时按阶段展开, 宿主零接线) + 链路标识中间件 `SnowCorrelationIdMiddleware`(对应 ABP 的 `AbpCorrelationIdMiddleware`) + 应用初始化上下文扩展 `GetApplicationBuilder()` 一族, 外加全部框架能力的契约与内存实现; 不含任何具体后端(EF / Redis / Hangfire / Serilog / MailKit / FluentFTP / ClosedXML / Elsa 由宿主按需引用) |
| `Snow.AspNetCore.Serilog` | Serilog 集成: 请求级租户/用户/链路标识自动进日志上下文; 已依赖 `Snow.AspNetCore`(与 ABP 的 `Volo.Abp.AspNetCore.Serilog` 依赖 `Volo.Abp.AspNetCore` 同形), 宿主不必再单独声明接线模块 |
| `Snow.AspNetCore.Mvc` | MVC 与自动 API: 应用服务按约定自动变成 Controller(路由/动词/参数绑定), 统一响应信封 `SnowApiResponse`, 异常与模型校验统一成 400/403/404/500 错误响应 |
| `Snow.OpenApi` | OpenAPI 文档与调试界面: 官方 Microsoft.AspNetCore.OpenApi 出文档 + Scalar 出界面, 自动收录全部自动 API, 可选 Bearer 认证方案, 自动加载程序集 XML 注释 |
| `Snow.AspNetCore.HealthChecks` | 健康检查: `AddSnowHealthCheck<T>` 登记, `MapSnowHealthChecks()` 暴露 JSON 端点(默认 `/health`) |
| `Snow.AspNetCore.SignalR` | SignalR: `SnowHub` 基类与 `[SnowHub]` 路径注解, Hub 自动扫描, `MapSnowHubs()` 挂端点 |
| `Snow.AspNetCore.RateLimiting` | 限流: 对微软自带 `AddRateLimiter`/`UseRateLimiter` 的薄封装, 无自有选项 |
| `Snow.AspNetCore.MultiTenancy` | 多租户 Web 适配层: 查询串 / 路由 / 请求头 / Cookie 四个租户出资者 + 多租户中间件(自注册到 `AfterAuthentication` 阶段, 请求级切换租户, 未知租户回 404), 已依赖 `Snow.MultiTenancy` |

### 领域与应用

| 包 | 用途 |
| --- | --- |
| `Snow.Domain` | 实体、聚合根、审计实体、软删除、领域事件、仓储契约、值对象 |
| `Snow.Application.Contracts` | 应用层契约: 实体 DTO、分页请求与分页结果 |
| `Snow.Application` | 应用层基元: 后台作业与模板渲染端口 |
| `Snow.Uow` | 工作单元与事务: `IUnitOfWork`、`[Transactional]` 注解、提交/回滚回调 |

### 持久化

| 包 | 用途 |
| --- | --- |
| `Snow.EntityFrameworkCore` | EF Core 集成: `SnowDbContext` 基类、连接串解析、按工作单元共享的上下文、ABP 式仓储、审计/软删除/领域事件拦截 |
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
| `Snow.BackgroundJobs.Hangfire` | 后台任务的 Hangfire 适配: 接管入队与调度两个端口, 带仪表盘 |
| `Snow.Excel` | Excel 导入导出契约: `IExcelService`、列映射注解 `[ExcelColumn]` / `[ExcelIgnore]`、导入导出选项, 含静默兜底实现(调用即报错) |
| `Snow.Excel.ClosedXML` | Excel 的 ClosedXML 实现: 对象集合或 `DataTable` 导出 xlsx(表头加粗/冻结/筛选/列宽), 按表头导回强类型列表, 提供原生 `XLWorkbook` 入口 |
| `Snow.Workflows` | 工作流契约: `ISnowWorkflowRunner`(按定义标识或类型启动)、结果快照 `SnowWorkflowResult`, 含静默兜底实现(调用即报错) |
| `Snow.Workflows.Elsa` | 工作流的 Elsa 3 实现: 接入 Elsa 引擎(内存或 EF Core / SQLite 持久化)、HTTP 端点与管理 API, 顶替契约包的静默实现 |
| `Snow.VirtualFileSystem` | 虚拟文件系统: 把多个物理目录与程序集内嵌资源合成一个 `IFileProvider`, 静态资源不必散落 |
| `Snow.Json` | JSON 序列化: 契约 `ISnowJsonSerializer` + 公共选项(`WriteIndented` / `IgnoreNullValues` / `AllowComments`) + **默认的 System.Text.Json 实现**(小驼峰、枚举转小驼峰字符串、忽略 null, 零第三方依赖), 对应 ABP 的 `Volo.Abp.Json` + `Volo.Abp.Json.SystemTextJson` |
| `Snow.Json.Newtonsoft` | JSON 的 Newtonsoft.Json 实现: 同样的驼峰与空值策略, 需要 Newtonsoft 行为(如 `$type` 保留)时装这个 |
| `Snow.ObjectMapping` | 对象映射契约: `IObjectMapper` 五个 Map 重载 + `source.Map<T>()` 链式扩展, 含静默兜底实现 |
| `Snow.ObjectMapping.Mapster` | 对象映射的 Mapster 实现(对应 ABP AutoMapper): `SnowMapsterProfile` 基类、`[SnowAutoMap]` 声明式映射、`AddProfile`/`AddMaps`、`Configurators`、`DefaultMaxDepth`、`ValidateMappings` 启动校验、可注入 `MapsterMapper.IMapper` |
| `Snow.Localization` | 本地化: 资源定义 + `IStringLocalizer` / `IStringLocalizerFactory` 实现, 按当前文化逐级回退 |
| `Snow.Settings` | 设置: 定义 + 值提供器链(默认值 / 配置 / 全局存储 / 租户)分层覆盖, 顺序由 `SnowSettingOptions.ValueProviders` 决定, `ISettingProvider` 读取 |
| `Snow.Features` | 功能开关: 定义 + 值提供器链, `IFeatureChecker.IsEnabledAsync` 判定 |
| `Snow.BlobStoring` | 对象存储: `IBlobContainer` / `IBlobContainer<T>` 读写二进制对象, 默认内存实现 |
| `Snow.DataSeeding` | 数据播种: 启动阶段按顺序执行全部 `IDataSeedContributor` |

### 安全与其它

| 包 | 用途 |
| --- | --- |
| `Snow.Aop.Castle` | AOP 实现: 基于 Castle DynamicProxy 的接口代理与真异步方法拦截 |
| `Snow.Security` | 当前用户抽象与基于 `ClaimsPrincipal` 的默认实现 |
| `Snow.Auditing` | 审计: `[Auditing]` 注解采集调用方 / 耗时 / 参数 / 异常, `IAuditPropertySetter` 填实体审计字段; 默认 `NullAuditingStore` 只写调试日志 |
| `Snow.Authentication` | 认证: 一个模块 `SnowAuthenticationModule`, 处理器是框架自带的 `JwtBearerHandler`。只有 `SecretKey` 必填(`Issuer` / `Audience` / `Expiration` 都有默认值), HS256 签发令牌、会话白名单、滑动续期(一直在用就一直在线)、踢人封禁在线数、注解鉴权、`TokenUtil` 门面 |
| `Snow.Authorization` | 权限: 定义树(自动扫描)、校验器、值提供器与动态策略, `[RequirePermission]` 注解即用 |
| `Snow.Emailing` | 邮件发送契约与配置(默认是静默实现, 不真发) |
| `Snow.MultiTenancy` | 多租户内核: 租户上下文与切换、租户配置与存储(默认从 `MultiTenancy:Tenants` 读)、解析出资者、租户级设置提供器 `T`, 默认关闭 |
| `Snow.Emailing.MailKit` | 邮件的 MailKit 实现: MimeKit 组装正文/附件, SMTP 走 SSL 或 STARTTLS 真投递 |
| `Snow.Ftping` | FTP 客户端契约与配置, 含静默兜底实现 `NullFtpClient`(写操作记 warning 并返 false) |
| `Snow.Ftping.FluentFTP` | FTP 的 FluentFTP 实现: 每次调用建连, 上传/下载/删除/建目录/取目录列表, 文件名编码可配 |
| `Snow.Validation` | 参数校验: `ISnowObjectValidator` 做 DataAnnotations 校验(含嵌套对象), `[SnowValidation]` 方法级拦截, 统一抛 `SnowValidationException` |
| `Snow.Validation.FluentValidation` | 校验的 FluentValidation 后端: 自动扫描 `AbstractValidator<T>`, 把 DataAnnotations 与 FluentValidation 合成一条管线 |
| `Snow.ExceptionHandling` | 异常处理: `IExceptionToErrorInfoConverter` 把异常翻译成 `SnowRemoteServiceErrorInfo`(错误码 / 消息 / 字段错误 / 附加数据) |

## 注解一览

| 注解 | 作用 |
| --- | --- |
| `[Cacheable("'user:' + #id", ExpirationSeconds = 60)]` | 方法结果缓存, Key / Condition / Unless 用表达式求值 |
| `[CacheEvict(typeof(UserCacheItem), "id")]` | 方法执行后失效缓存(支持 AllEntries) |
| `[DistributedLock("'stock:' + id", WaitSeconds = 3)]` | 方法级分布式锁, 支持 Prefix / Condition / LeaseSeconds / ThrowOnLost |
| `[Transactional]` | 方法级工作单元与事务, 支持 RequiresNew |
| `[Auditing]` | 方法级审计日志(调用方 / 耗时 / 参数 / 异常), 由 `IAuditingStore` 落库 |
| `[RequirePermission("Snow.Orders.Create")]` | 要求当前用户拥有指定权限, 策略动态生成并缓存 |
| `[TokenCheckLogin]` / `[TokenCheckRole(...)]` | 登录 / 角色校验, 策略名 `snowauth:*` 动态生成; 权限用 `[RequirePermission(...)]` |
| `app.UseSnowTokenExceptionHandling()` | 把登录态异常翻译成 401/403(单独使用 `Snow.Authentication` 时才需要; 正常由模块登记到 `Outermost` 阶段自动挂) |

## 许可

Apache License 2.0
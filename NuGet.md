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
```

## 快速开始

```csharp
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// 加载启动模块及其全部依赖, 并立即执行服务注册阶段
builder.Services.AddApplication<MyModule>();

WebApplication app = builder.Build();

// 驱动各模块的初始化阶段
app.Services.Initialize();

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

业务类型通过 `ISingletonDependency` / `IScopedDependency` / `ITransientDependency` 或 `[Dependency]`
参与约定注册; 接口名与实现类名去掉 `I ` 前缀一致即自动暴露为该接口的实现。

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
| `Snow.Core` | 内核: 模块系统、约定式依赖注入、AOP 拦截契约、表达式求值、数据过滤、System 扩展方法 |
| `Snow.AspNetCore` | Web 宿主入口包: 一次引用拿到全部框架能力 |
| `Snow.AspNetCore.Serilog` | Serilog 集成: 请求级用户/租户/会话字段自动进日志上下文 |

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

### 基础设施

| 包 | 用途 |
| --- | --- |
| `Snow.EventBus` | 事件总线: 事件契约(含处理器接口)、发布策略、处理器包装与模块 |
| `Snow.Messaging` | 消息总线: 命令/查询契约(含管道行为接口)与默认派发实现 |
| `Snow.Caching` | 分布式缓存: 强类型包装 + `[Cacheable]` / `[CacheEvict]` 注解 |
| `Snow.Caching.StackExchangeRedis` | 缓存的 Redis 后端 |
| `Snow.DistributedLocking` | 分布式锁: 契约 + 进程内默认实现 + Redisson 式看门狗 + `[DistributedLock]` 注解 |
| `Snow.DistributedLocking.Redis` | 分布式锁的 Redis 后端(SET NX PX + Lua, 无第三方锁库) |
| `Snow.BackgroundJobs.Abstractions` | 后台任务契约: 任务接口、记录、存储/序列化契约、执行器与调度端口 |
| `Snow.BackgroundJobs` | 后台任务默认实现: 管理器、进程内存储、JSON 序列化、轮询 worker |
| `Snow.BackgroundJobs.HangFire` | 后台任务的 Hangfire 适配: 接管管理器/调度器/命令投递三个端口 |

### 安全与其它

| 包 | 用途 |
| --- | --- |
| `Snow.Aop.Castle` | AOP 实现: 基于 Castle DynamicProxy 的接口代理与真异步方法拦截 |
| `Snow.Security` | 当前用户抽象与基于 `ClaimsPrincipal` 的默认实现 |
| `Snow.Security.SaToken` | Sa-Token 风格登录态: 令牌 + 会话存储 + 注解鉴权 + `StpUtil` 门面 |
| `Snow.Authorization` | 权限定义树、权限校验器、多值提供器与动态策略提供器 |
| `Snow.Emailing` | 邮件发送契约与配置(默认是静默实现, 不真发) |
| `Snow.Emailing.MailKit` | 邮件的 MailKit 实现: MimeKit 组装正文/附件, SMTP 走 SSL 或 STARTTLS 真投递 |
| `Snow.Ftping` | FTP 客户端契约与配置, 含静默兜底实现 `NullFtpClient`(写操作记 warning 并返 false) |
| `Snow.Ftping.FluentFTP` | FTP 的 FluentFTP 实现: 每次调用建连, 上传/下载/删除/建目录/取目录列表, 文件名编码可配 |

## 注解一览

| 注解 | 作用 |
| --- | --- |
| `[Cacheable("'user:' + #id", ExpirationSeconds = 60)]` | 方法结果缓存, Key / Condition / Unless 用表达式求值 |
| `[CacheEvict(typeof(UserCacheItem), "id")]` | 方法执行后失效缓存(支持 AllEntries) |
| `[DistributedLock("'stock:' + id", WaitSeconds = 3)]` | 方法级分布式锁, 支持 Prefix / Condition / LeaseSeconds / ThrowOnLost |
| `[Transactional]` | 方法级工作单元与事务, 支持 RequiresNew |

## 许可

Apache License 2.0

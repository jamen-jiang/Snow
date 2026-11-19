# Rongia Framework

Rongia 是一套 .NET 11 的模块化应用框架, 提供模块系统、约定式依赖注入、AOP 拦截、事件总线、
命令/查询派发、表达式引擎与分布式缓存门面。

## 包一览

| 包 | 用途 |
| --- | --- |
| Rongia | 名称占位包: 不含任何代码, 只引用 Rongia.Core |
| Rongia.AspNetCore | Web 宿主入口包: 一次引用拿到 ASP.NET Core 场景下的全部框架能力 |
| Rongia.AspNetCore.Serilog | Serilog 集成: 请求级用户/租户/会话字段自动进日志上下文 |
| Rongia.Core | 框架内核: 模块系统、约定式依赖注入、AOP 拦截契约、消息契约 |
| Rongia.Shared | 分页请求/响应等跨层共享 DTO 与枚举扩展 |
| Rongia.Domain | 领域层基元: 实体、聚合根、审计实体、领域事件、仓储契约、值对象 |
| Rongia.Application.Contracts | 应用层契约: 实体 DTO、分页结果、请求 DTO |
| Rongia.Application | 应用层基元: 后台作业与模板渲染端口 |
| Rongia.EventBus.Abstractions | 事件与事件处理器契约 |
| Rongia.EventBus | 事件总线实现与发布策略 |
| Rongia.Messaging | 命令/查询派发与管道行为 |
| Rongia.Expressions | 基于 DynamicExpresso 的表达式引擎 |
| Rongia.Caching | 强类型分布式缓存门面与 [Cacheable] / [CacheEvict] |
| Rongia.Caching.StackExchangeRedis | Redis 缓存后端 |
| Rongia.Aop.Castle | 基于 Castle DynamicProxy 的 AOP 实现 |
| Rongia.Authorization | 权限定义与授权检查 |
| Rongia.Security | 当前用户与安全上下文 |
| Rongia.Security.SaToken | SaToken 安全上下文实现 |
| Rongia.DistributedLock.Abstractions | 分布式锁契约 |
| Rongia.DistributedLock | 分布式锁实现 |
| Rongia.Emailing | 邮件发送契约与配置 |
| Rongia.HangFire | 后台任务: Hangfire 集成, 实现 IBackgroundJob / IBackgroundExecutor 端口并带仪表盘 |
| Rongia.Ftp | FTP 客户端契约与配置 |

## 快速开始

```csharp
ServiceCollection services = new();
services.AddSingleton<IConfiguration>(configuration);

RongiaApplication application = services.AddApplication<MyModule>();

using ServiceProvider provider = services.BuildServiceProvider();
await application.InitializeAsync(provider);
```

模块类继承 `RongiaModule` 并通过 `[DependsOn]` 声明依赖, 业务类型通过 `ISingletonDependency` / `IScopedDependency` /
`ITransientDependency` 或 `[Dependency]` 参与约定注册。

## 许可

Apache License 2.0

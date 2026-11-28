using Elsa.Extensions;
using Hangfire;
using Hangfire.Storage.SQLite;
using Snow.Castle;
using Snow.AspNetCore;
using Snow.AspNetCore.MultiTenancy;
using Snow.AspNetCore.Mvc;
using Snow.AspNetCore.RateLimiting;
using Snow.AspNetCore.Serilog;
using Snow.AspNetCore.SignalR;
using Snow.AspNetCore.Authentication;
using Snow.Authorization;
using Snow.BackgroundJobs;
using Snow.BackgroundJobs.Hangfire;
using Snow.Caching.StackExchangeRedis;
using Snow.Validation;
using Snow.Data;
using Medallion.Threading;
using Medallion.Threading.Redis;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Snow.DistributedLocking;
using StackExchange.Redis;
using Snow.Emailing.MailKit;
using Snow.EntityFrameworkCore;
using Snow.EventBus;
using Snow.Ftping.FluentFTP;
using Snow.Http.Client;
using Snow.Json;
using Snow.Json.Newtonsoft;
using Snow.Localization;
using Snow.Messaging;
using Snow.Modularity;
using Snow.MultiTenancy;
using Snow.ObjectMapping.Mapster;
using Snow.OpenApi;
using Snow.Sample.Web.Workflows;
using Snow.Security;
using Snow.Timing;
using Snow.Tracing;
using Snow.Uow;
using Snow.Validation.FluentValidation;
using Snow.Workflows.Elsa;

namespace Snow.Sample.Web;

/// <summary>
/// 全模块示例启动模块: 一次性 <c>DependsOn</c> 框架全部能力模块与各实现提供者。
/// <para>按 Snow 约定, 管道由启动模块在 <see cref="OnApplicationInitialization"/> 里手写。</para>
/// </summary>
[DependsOn(
    typeof(SnowCastleCoreModule),
    typeof(SnowAspNetCoreModule),
    typeof(SnowAspNetCoreMultiTenancyModule),
    typeof(SnowAspNetCoreMvcModule),
    typeof(SnowRateLimitingModule),
    typeof(SnowAspNetCoreSerilogModule),
    typeof(SnowSignalRModule),
    typeof(SnowAuthenticationModule),
    typeof(SnowAuthorizationModule),
    typeof(BackgroundJobsModule),
    typeof(SnowHangfireModule),
    typeof(SnowCachingStackExchangeRedisModule),
    typeof(SnowDataModule),
    typeof(SnowEmailingMailKitModule),
    typeof(SnowEntityFrameworkCoreModule),
    typeof(SnowEntityFrameworkCoreMySqlModule),
    typeof(SnowEntityFrameworkCorePostgreSqlModule),
    typeof(SnowEntityFrameworkCoreSqlServerModule),
    typeof(SnowEntityFrameworkCoreSqliteModule),
    typeof(SnowEventBusModule),
    typeof(SnowFtpingFluentFTPModule),
    typeof(SnowHttpClientModule),
    typeof(SnowJsonModule),
    typeof(SnowJsonNewtonsoftModule),
    typeof(SnowLocalizationModule),
    typeof(SnowMessagingModule),
    typeof(SnowMultiTenancyModule),
    typeof(SnowObjectMappingMapsterModule),
    typeof(SnowOpenApiModule),
    typeof(SnowSecurityModule),
    typeof(SnowTimingModule),
    typeof(SnowTracingModule),
    typeof(SnowUowModule),
    typeof(SnowValidationFluentValidationModule),
    typeof(SnowElsaModule)
)]
public sealed class SampleModule : SnowModule
{
    /// <inheritdoc />
    /// <param name="context">上下文</param>
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // 框架模块不读配置节(与 ABP 一致): 选项由宿主给出。
        // 这些模块在 ConfigureServices 里就要用选项(扫描校验器 / 建映射配置 / 决定端点), 所以必须在这里绑 ——
        // PreConfigureServices 全部跑完才开始执行任何模块的 ConfigureServices。
        Configure<SnowCorrelationIdOptions>(context.Configuration.GetSection("CorrelationId"));
        Configure<SnowFluentValidationOptions>(context.Configuration.GetSection("Validation:FluentValidation"));
        Configure<SnowHttpClientOptions>(context.Configuration.GetSection("HttpClient"));
        Configure<SnowJsonOptions>(context.Configuration.GetSection("Json"));
        Configure<SnowOpenApiOptions>(context.Configuration.GetSection("OpenApi"));
        Configure<SnowValidationOptions>(context.Configuration.GetSection("Validation"));
        Configure<SnowCachingRedisOptions>(context.Configuration.GetSection("Redis"));
        // ==== Elsa 选项示例: SnowElsaOptions 的每一项都在这里用代码点一遍 ====
        // 模块不读配置节, 选项全由宿主给。这里用 SnowModule.Configure<TOptions>(Action<TOptions>) 直接写代码。
        string contentRootPath = context.Services.GetHostingEnvironment().ContentRootPath;
        Configure<SnowElsaOptions>(options =>
        {
            // ① 持久化: 非空即落库(SQLite 文件), 留空则纯内存、重启即丢。
            //    相对路径要按内容根展开 —— 否则 Elsa 只在进程工作目录下建库, 换个启动方式就换了一个库。
            options.ConnectionString = ResolveSqlitePath("Data Source=DataBase/snow.db", contentRootPath);

            // ② 工作流 HTTP 端点: 关掉时管理 API 仍在, 宿主也不用挂 app.UseSnowWorkflows()
            options.EnableHttp = true;
            options.HttpBasePath = "/workflows";

            // ③ 管理 API(/elsa/api): 工作流定义的增删改查与手动触发
            options.EnableApi = true;

            // ④ 代码方式定义的工作流: 逐个加进来, 模块会用 services.AddWorkflow(type) 注册;
            //    面板与管理 API 里按定义名启动。这里放示例宿主里的 HelloWorldWorkflow。
            options.WorkflowTypes.Add(typeof(HelloWorldWorkflow));

            // ⑤ 最后一道口子: 直接拿 Elsa 的 IModule 追加特性 —— 模块内置的表达式/缓存/持久化/HTTP/管理 API
            //    之后执行, 所以能改这些内置项; Elsa 的特性配置是追加式回调, 再调一次即"内置之后补一刀"。
            options.ConfigureElsa = elsa =>
            {
                // 身份(Identity): 一样是代码写法 —— UseIdentity 的回调里给各选项赋值。
                elsa.UseIdentity(identity =>
                {
                    // 管理员账号: 内置 AdminUserProvider 的代码版, 用户名/密码/角色都在这儿写。
                    // 想少写两行可换成 UseDevelopmentAdminUserProvider(), 它等价于 admin/password, 只适合开发环境。
                    identity.UseAdminUserProvider(admin =>
                    {
                        admin.UserId = "admin";
                        admin.UserName = "admin";
                        admin.Password = "ChangeMe!123";
                        admin.Roles = ["admin"];
                    });

                    // 令牌: 签名密钥不配就用 Elsa 的默认值, 这里用代码改有效期。
                    identity.TokenOptions = token =>
                    {
                        token.AccessTokenLifetime = TimeSpan.FromHours(8);
                        token.RefreshTokenLifetime = TimeSpan.FromDays(7);
                    };

                    // 其它身份来源也是同一写法, 按需替换(默认走内存存储):
                    //   identity.UseConfigurationBasedUserProvider(users =>
                    //       users.Users.Add(new User { Name = "alice", Roles = ["admin"] }));
                    //   identity.UseConfigurationBasedRoleProvider(roles =>
                    //       roles.Roles.Add(new Role { Name = "editor", Permissions = ["read", "write"] }));
                    //   identity.UseConfigurationBasedApplicationProvider(apps =>
                    //       apps.Applications.Add(new Application { ClientId = "sample-app", Name = "示例应用" }));
                    //   identity.UseStoreBasedUserProvider() / UseStoreBasedRoleProvider() / UseStoreBasedApplicationProvider()
                });
            };
        });
    }

    /// <inheritdoc />
    /// <param name="context">上下文</param>
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // 分布式锁: 与 ABP 一样, 框架包不带任何远程后端 —— 宿主自己引用 DistributedLock.Redis 并注册锁提供者。
        context.Services.TryAddSingleton<IDistributedLockProvider>(provider =>
            new RedisDistributedSynchronizationProvider(provider.GetRequiredService<IConnectionMultiplexer>().GetDatabase()));

        // 认证选项不读配置节由模块自己决定, 这里按示例宿主的习惯显式绑一节
        Configure<SnowAuthenticationOptions>(context.Configuration.GetSection("Authentication"));

        // Hangfire 的存储由宿主选: 框架不绑定任何存储库(与 ABP 一样), 用 Hangfire 自己的 AddHangfire 追加。
        // 模块已经给过兼容级别与序列化器这些设置, 这里后加的存储排在后面, 持久化存储就盖掉默认值。
        // 换库换包与 UseXxxStorage 即可: Hangfire.SqlServer / Hangfire.PostgreSql / Hangfire.MySql。
        // 注意: SQLite 提供程序把这个参数当"数据库文件名"; 换成关系库时填真实连接串。
        // 路径相对宿主的内容根, 配成 DataBase/xxx.db 就落在本项目目录下的 DataBase/ 里。
        string hangfireConnectionString = context.Configuration.GetConnectionString("Hangfire")
            ?? throw new InvalidOperationException("缺少配置 ConnectionStrings:Hangfire: Hangfire 需要持久化存储的位置(文件路径或连接串)。");
        string contentRootPath = context.Services.GetHostingEnvironment().ContentRootPath;
        context.Services.AddHangfire(configuration =>
            configuration.UseSQLiteStorage(ResolveSqlitePath(hangfireConnectionString, contentRootPath)));
    }

    // SQLite 存储把参数当文件名, 而 "Data Source=" 形态是给关系库用的: 两种都认, 相对路径按内容根展开,
    // 并把目录建出来 —— 否则 SQLite 只在进程工作目录下找文件, 换个启动方式就换了一个库。
    private static string ResolveSqlitePath(string connectionString, string contentRootPath)
    {
        foreach (string key in new[] { "Data Source", "Filename" })
        {
            string prefix = key + "=";
            int index = connectionString.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
            {
                continue;
            }

            int start = index + prefix.Length;
            int end = connectionString.IndexOf(';', start);
            string value = (end < 0 ? connectionString[start..] : connectionString[start..end]).Trim();
            return connectionString[..start] + ExpandPath(value, contentRootPath) + (end < 0 ? string.Empty : connectionString[end..]);
        }

        return ExpandPath(connectionString.Trim(), contentRootPath);
    }

    private static string ExpandPath(string path, string contentRootPath)
    {
        if (path.Length == 0 || path == ":memory:" || Path.IsPathRooted(path))
        {
            return path;
        }

        string fullPath = Path.GetFullPath(Path.Combine(contentRootPath, path));
        string? directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        return fullPath;
    }

    /// <inheritdoc />
    /// <param name="context">上下文</param>
    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Snow 约定: 模块不铺管道, 整条管道由启动模块在这里按行序手写(对齐 ABP 模板)。
        // 下面把框架各能力包提供的 UseSnowXxx 逐一接上, 书写顺序即中间件的嵌套顺序(先挂的在最外层)。
        IApplicationBuilder app = context.GetApplicationBuilder();

        // 0) 异常处理: 最外层, 把后续管道抛出的所有异常翻译成统一错误信封(JSON)
        app.UseSnowExceptionHandling();

        // 1) 链路标识: 必须最外层 —— 认证失败 / 授权拒绝 / 路由没匹配(404) 的响应也要带回 X-Correlation-Id
        app.UseSnowCorrelationId();

        // 2) 请求级日志上下文增强: 尽量靠外, 让后续中间件与业务日志都带上 用户 / 租户 / 链路;
        //    要能读到链路标识, 所以排在 UseSnowCorrelationId 之内
        app.UseSnowSerilogEnrichers();

        // 4) 路由: 后续限流与端点映射都依赖它先把 endpoint 解析出来
        app.UseRouting();

        // 5) 限流: 必须在 UseRouting() 之后 —— 限流策略挂在端点元数据上, 路由之前读不到会静默失效
        app.UseSnowRateLimiting();

        // 6) 认证
        app.UseAuthentication();

        // 7) 多租户解析: 必须在 UseAuthentication() 之后 —— 路由出资者要读路由值, 当前用户出资者要读已认证主体;
        //    未启用多租户时中间件原样放行, 零开销
        app.UseSnowMultiTenancy();

        // 8) 授权
        app.UseAuthorization();

        // 9) 后台作业仪表盘(/hangfire): 授权之后、端点之前 —— 终端分支, 早于授权就对匿名请求可见
        app.UseSnowHangfireDashboard();

        // 10) 工作流 HTTP 端点(/workflows): 同上, 终端分支, 落点必须在授权之后
        app.UseSnowWorkflows();

        // 11) 端点映射: 管道最后一步, 统一展开各模块登记的端点(含 OpenAPI / Scalar 文档)
        app.UseSnowEndpoints();
    }
}

using Elsa.Extensions;
using Elsa.Http.Features;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Management;
using Elsa.Persistence.EFCore.Modules.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Snow.DependencyInjection;
using Snow.Modularity;

namespace Snow.Workflows.Elsa;

/// <summary>
/// Elsa 模块: 加载 Elsa 引擎并接上本框架的 ISnowWorkflowRunner。
/// <para>工作流 HTTP 中间件由宿主自己挂: 在启动模块的 OnApplicationInitialization 里调
/// <c>context.GetApplicationBuilder().UseSnowWorkflows()</c>, 放在授权之后: 它是终端分支, 早于授权就对匿名请求可见。</para>
/// <para><see cref="SnowElsaOptions.EnableHttp"/> 为 false 时不要挂即可, 管理 API 仍在。</para>
/// <para>运行工作流所需的最小特性集(表达式、JavaScript 引擎、内存缓存、管理与运行时缓存)总是挂上;
/// 想省依赖或用别的实现, 在 <see cref="SnowElsaOptions.ConfigureElsa"/> 里补。</para>
/// </summary>
[DependsOn(typeof(SnowWorkflowsModule))]
public sealed class SnowElsaModule : SnowModule
{
    /// <inheritdoc />
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        IServiceCollection services = context.Services;

        // 选项不读配置节(与 ABP 一致): 宿主在 PreConfigureServices 里 Configure<SnowElsaOptions>(...)
        SnowElsaOptions options = services.GetOptions<SnowElsaOptions>();

        // 收尾阶段挂中间件要读 EnableHttp, 把算好的最终选项放进容器
        services.AddSingleton(options);

        bool useSqlite = !string.IsNullOrWhiteSpace(options.ConnectionString);

        foreach (Type workflowType in options.WorkflowTypes)
        {
            services.AddWorkflow(workflowType);
        }

        services.AddElsa(elsa =>
        {
            elsa.UseExpressions(_ => { });
            elsa.UseJavaScript((global::Elsa.Expressions.JavaScript.Features.JavaScriptFeature _) => { });
            elsa.UseMemoryCache(_ => { });

            elsa.UseWorkflowManagement(management =>
            {
                if (useSqlite)
                {
                    management.UseEntityFrameworkCore(ef =>
                    {
                        ef.UseSqlite(options.ConnectionString!);

                        // 启动即迁移: 表结构与包版本对不上时先建/补, 免得第一次跑工作流才炸
                        ef.RunMigrations = true;
                    });
                }

                management.UseCache(_ => { });
            });

            elsa.UseWorkflowRuntime(runtime =>
            {
                if (useSqlite)
                {
                    runtime.UseEntityFrameworkCore(ef =>
                    {
                        ef.UseSqlite(options.ConnectionString!);
                        ef.RunMigrations = true;
                    });
                }

                runtime.UseCache(_ => { });
                runtime.UseDefaultRuntime(_ => { });
            });

            if (options.EnableHttp)
            {
                elsa.UseHttp(http =>
                {
                    http.UseCache(_ => { });

                    http.ConfigureHttpOptions = httpOptions => httpOptions.BasePath = options.HttpBasePath;
                });
            }

            if (options.EnableApi)
            {
                elsa.UseWorkflowsApi();
            }

            options.ConfigureElsa?.Invoke(elsa);
        });
    }
}

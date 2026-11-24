using Elsa.Extensions;
using Elsa.Http.Features;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Management;
using Elsa.Persistence.EFCore.Modules.Runtime;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Snow.DependencyInjection;
using Snow.Modularity;

namespace Snow.Workflows.Elsa;

/// <summary>
/// Elsa 模块: 加载 Elsa 引擎并接上本框架的 ISnowWorkflowRunner。
/// <para>工作流 HTTP 中间件由宿主自己挂: 在启动模块的 OnApplicationInitialization 里调
/// <c>context.GetApplicationBuilder().UseSnowWorkflows()</c>, 放在授权之后: 它是终端分支, 早于授权就对匿名请求可见。</para>
/// <para><see cref="SnowElsaOptions.EnableHttp"/> 为 false 时不要挂即可, 管理 API 仍在。</para>
/// </summary>
[DependsOn(typeof(SnowWorkflowsModule))]
public sealed class SnowElsaModule : SnowModule
{
    /// <inheritdoc />
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        IServiceCollection services = context.Services;

        services.Configure<SnowElsaOptions>(options => services.GetConfiguration().GetSection("Workflows:Elsa").Bind(options));
        SnowElsaOptions options = services.GetOptions<SnowElsaOptions>();

        // 收尾阶段挂中间件要读 EnableHttp, 把算好的最终选项放进容器
        services.AddSingleton(options);

        bool useSqlite = options.Database == SnowElsaDatabaseProvider.Sqlite && !string.IsNullOrWhiteSpace(options.ConnectionString);

        foreach (Type workflowType in options.WorkflowTypes)
        {
            services.AddWorkflow(workflowType);
        }

        services.AddElsa(elsa =>
        {
            // 运行工作流所需的最小特性集: 表达式、JS 引擎与内存缓存。取舍项(如身份)不放在这里。
            if (options.UseDefaultFeatures)
            {
                elsa.UseExpressions(_ => { });

                if (options.UseJavaScript)
                {
                    elsa.UseJavaScript((global::Elsa.Expressions.JavaScript.Features.JavaScriptFeature _) => { });
                }

                elsa.UseMemoryCache(_ => { });
            }

            elsa.UseWorkflowManagement(management =>
            {
                if (useSqlite)
                {
                    management.UseEntityFrameworkCore(ef =>
                    {
                        ef.UseSqlite(options.ConnectionString!);
                        ef.RunMigrations = options.RunMigrations;
                    });
                }

                options.ConfigureWorkflowManagement?.Invoke(management);

                if (options.UseDefaultFeatures)
                {
                    management.UseCache(_ => { });
                }
            });

            elsa.UseWorkflowRuntime(runtime =>
            {
                if (useSqlite)
                {
                    runtime.UseEntityFrameworkCore(ef =>
                    {
                        ef.UseSqlite(options.ConnectionString!);
                        ef.RunMigrations = options.RunMigrations;
                    });
                }

                options.ConfigureWorkflowRuntime?.Invoke(runtime);

                if (options.UseDefaultFeatures)
                {
                    runtime.UseCache(_ => { });
                    runtime.UseDefaultRuntime(_ => { });
                }
            });

            if (options.EnableHttp)
            {
                elsa.UseHttp(http =>
                {
                    if (options.UseDefaultFeatures)
                    {
                        http.UseCache(_ => { });
                    }

                    http.ConfigureHttpOptions = httpOptions =>
                    {
                        if (options.HttpBaseUrl is not null)
                        {
                            httpOptions.BaseUrl = options.HttpBaseUrl;
                        }

                        httpOptions.BasePath = options.HttpBasePath;
                    };
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

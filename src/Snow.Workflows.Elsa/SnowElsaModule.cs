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

        SnowElsaOptions options = new();
        IConfigurationSection section = services.GetConfiguration().GetSection("Workflows:Elsa");
        if (section.Exists())
        {
            section.Bind(options);
        }

        SnowElsaOptionsSetup? setup = services
            .FirstOrDefault(descriptor => descriptor.ServiceType == typeof(SnowElsaOptionsSetup))
            ?.ImplementationInstance as SnowElsaOptionsSetup;
        setup?.Apply(options);

        // 收尾阶段挂中间件要读 EnableHttp, 把算好的最终选项放进容器
        services.AddSingleton(options);

        bool useSqlite = options.Database == SnowElsaDatabaseProvider.Sqlite && !string.IsNullOrWhiteSpace(options.ConnectionString);

        foreach (Type workflowType in options.WorkflowTypes)
        {
            services.AddWorkflow(workflowType);
        }

        services.AddElsa(elsa =>
        {
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
            });

            if (options.EnableHttp)
            {
                elsa.UseHttp(http =>
                {
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

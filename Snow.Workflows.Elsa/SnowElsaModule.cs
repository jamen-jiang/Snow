using Elsa.Extensions;
using Elsa.Http.Features;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Management;
using Elsa.Persistence.EFCore.Modules.Runtime;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Snow.AspNetCore;
using Snow.DependencyInjection;
using Snow.Modularity;

namespace Snow.Workflows.Elsa;

/// <summary>
/// Elsa 模块: 加载 Elsa 引擎并接上本框架的 ISnowWorkflowRunner。
/// </summary>
[DependsOn(typeof(SnowWorkflowsModule), typeof(SnowAspNetCoreModule))]
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

        // Elsa 的工作流端点要路由值, 挂在路由之后
        Configure<SnowPipelineOptions>(options => options.AddFilter(new SnowPipelineFilter(
            "Snow.Workflows.Elsa",
            postRouting: app => app.UseSnowWorkflows())));

    }
}

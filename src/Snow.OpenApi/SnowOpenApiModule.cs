using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using Microsoft.AspNetCore.Routing;
using Snow.AspNetCore;
using Snow.DependencyInjection;
using Snow.Modularity;

namespace Snow.OpenApi;

/// <summary>
/// OpenAPI 文档模块: 用官方生成器产出文档, Scalar 提供调试界面
/// <para>对应 ABP 的 Volo.Abp.Swashbuckle, 只是把 Swashbuckle 换成 Microsoft.AspNetCore.OpenApi + Scalar。</para>
/// </summary>
[DependsOn(typeof(Snow.AspNetCore.Mvc.SnowMvcModule))]
public sealed class SnowOpenApiModule : SnowModule
{

    /// <summary>
    /// 登记文档服务与文档生成器的三段加工
    /// </summary>
    /// <param name="context">服务配置上下文</param>
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.Services.Configure<SnowOpenApiOptions>(options => context.Services.GetConfiguration().GetSection("OpenApi").Bind(options));
        SnowOpenApiOptions openApi = context.Services.GetOptions<SnowOpenApiOptions>();

        context.Services.AddSingleton(openApi);

        if (!openApi.Enabled)
        {
            return;
        }

        SnowXmlComments xmlComments = SnowXmlComments.Load(openApi.IncludeXmlComments ? GetXmlCommentPaths(context.Services) : []);

        context.Services.AddOpenApi(openApi.Version, options =>
        {
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info = new OpenApiInfo
                {
                    Title = openApi.Title,
                    Version = openApi.Version,
                    Description = openApi.Description,
                };

                if (!openApi.AddJwtBearerSecurity)
                {
                    return Task.CompletedTask;
                }

                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
                document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Name = "Authorization",
                    Description = "在此填入访问令牌, 只填令牌本身, 不要加 Bearer 前缀。",
                };

                document.Security ??= [];
                document.Security.Add(new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference("Bearer", document)] = [],
                });

                return Task.CompletedTask;
            });

            options.AddOperationTransformer((operation, transformerContext, _) =>
            {
                if (transformerContext.Description.ActionDescriptor is ControllerActionDescriptor descriptor)
                {
                    operation.Tags ??= new HashSet<OpenApiTagReference>();
                    if (operation.Tags.Count == 0)
                    {
                        operation.Tags.Add(new OpenApiTagReference(descriptor.ControllerName, transformerContext.Document));
                    }

                    xmlComments.ApplyToOperation(operation, descriptor.MethodInfo);
                }

                return Task.CompletedTask;
            });

            options.AddSchemaTransformer((schema, schemaContext, _) =>
            {
                xmlComments.ApplyToSchema(schema, schemaContext.JsonTypeInfo.Type);

                return Task.CompletedTask;
            });
        });

        // 端点映射交给统一挂载: 与 ABP 的 EndpointConfigureActions 同形
        Configure<SnowEndpointRouterOptions>(options =>
            options.EndpointConfigureActions.Add(endpointContext =>
                SnowOpenApiApplicationBuilderExtensions.MapOpenApiEndpoints(endpointContext.Endpoints, openApi)));
    }

    private static List<string> GetXmlCommentPaths(IServiceCollection services)
    {
        List<string> paths = [];
        HashSet<string> visited = new(StringComparer.OrdinalIgnoreCase);

        IEnumerable<Assembly> assemblies = services
            .GetSingletonInstance<IModuleContainer>()
            .Modules
            .SelectMany(module => module.AllAssemblies)
            .Append(Assembly.GetEntryAssembly()!)
            .Where(assembly => assembly is not null)
            .Distinct();

        foreach (Assembly assembly in assemblies)
        {
            string? location = assembly.Location;
            if (string.IsNullOrEmpty(location))
            {
                continue;
            }

            string xmlPath = Path.ChangeExtension(location, ".xml");
            if (File.Exists(xmlPath) && visited.Add(xmlPath))
            {
                paths.Add(xmlPath);
            }
        }

        return paths;
    }
}

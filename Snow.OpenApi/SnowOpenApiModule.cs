using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
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

        SnowSwaggerOptions swagger = BuildOptions(context.Services);

        context.Services.AddSingleton(swagger);
        context.Services.Configure<SnowSwaggerOptions>(options =>
        {
            options.Enabled = swagger.Enabled;
            options.Title = swagger.Title;
            options.Version = swagger.Version;
            options.Description = swagger.Description;
            options.RoutePrefix = swagger.RoutePrefix;
            options.IncludeXmlComments = swagger.IncludeXmlComments;
            options.AddJwtBearerSecurity = swagger.AddJwtBearerSecurity;
        });

        if (!swagger.Enabled)
        {
            return;
        }

        SnowXmlComments xmlComments = SnowXmlComments.Load(swagger.IncludeXmlComments ? GetXmlCommentPaths(context.Services) : []);

        context.Services.AddOpenApi(swagger.Version, options =>
        {
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info = new OpenApiInfo
                {
                    Title = swagger.Title,
                    Version = swagger.Version,
                    Description = swagger.Description,
                };

                if (!swagger.AddJwtBearerSecurity)
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
    }

    private static SnowSwaggerOptions BuildOptions(IServiceCollection services)
    {
        SnowSwaggerOptions options = new();

        IConfiguration? configuration = services.GetConfiguration();
        IConfigurationSection section = configuration?.GetSection("Swagger") ?? new ConfigurationBuilder().Build().GetSection("Swagger");
        if (section.Exists())
        {
            section.Bind(options);
        }

        services
            .Where(descriptor => descriptor.ServiceType == typeof(SnowSwaggerOptionsSetup))
            .Select(descriptor => descriptor.ImplementationInstance)
            .OfType<SnowSwaggerOptionsSetup>()
            .FirstOrDefault()
            ?.Apply(options);

        return options;
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

using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Snow.DependencyInjection;
using Snow.Modularity;

namespace Snow.AspNetCore.SignalR;

/// <summary>
/// SignalR 模块: 登记实时通信服务, 并在约定扫描时收集 Hub
/// <para>依赖 <see cref="SnowAspNetCoreModule"/>: Hub 端点挂在 ASP.NET Core 上。</para>
/// <para>Hub 端点由模块声明, 框架在全部模块初始化完成之后统一映射, 宿主一行不用写。</para>
/// </summary>
[DependsOn(typeof(SnowAspNetCoreModule))]
public sealed class SnowSignalRModule : SnowModule
{
    /// <inheritdoc />
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        SnowSignalROptions options = new();
        IConfigurationSection section = context.Services.GetConfiguration().GetSection("SignalR");
        if (section.Exists())
        {
            section.Bind(options);
        }

        SnowHubRegistry registry = SnowSignalRServiceCollectionExtensions.GetOrCreateRegistry(context.Services);

        if (options.AutoDiscoverHubs)
        {
            foreach (Assembly assembly in GetModuleAssemblies(context.Services))
            {
                foreach (Type type in GetHubTypes(assembly))
                {
                    registry.Add(type, SnowHubPath.For(type));
                }
            }
        }

        context.Services.AddSingleton(options);
        context.Services.Configure<SnowSignalROptions>(current =>
        {
            current.Enabled = options.Enabled;
            current.AutoDiscoverHubs = options.AutoDiscoverHubs;
            current.EnableDetailedErrors = options.EnableDetailedErrors;
            current.MaximumReceiveMessageSize = options.MaximumReceiveMessageSize;
        });

        if (!options.Enabled)
        {
            return;
        }

        context.Services.AddSignalR(signalR =>
        {
            signalR.EnableDetailedErrors = options.EnableDetailedErrors;
            if (options.MaximumReceiveMessageSize is { } size)
            {
                signalR.MaximumReceiveMessageSize = size;
            }
        });

        Configure<SnowEndpointRouterOptions>(current =>
            current.EndpointConfigureActions.Add(endpointContext =>
                SnowSignalRApplicationBuilderExtensions.MapHubs(endpointContext.Endpoints, registry)));
    }

    private static IEnumerable<Type> GetHubTypes(Assembly assembly)
    {
        Type[] types;
        try
        {
            types = assembly.GetExportedTypes();
        }
        catch (NotSupportedException)
        {
            yield break;
        }
        catch (FileNotFoundException)
        {
            yield break;
        }

        foreach (Type type in types)
        {
            if (type.IsClass && !type.IsAbstract && !type.IsGenericTypeDefinition && typeof(Hub).IsAssignableFrom(type))
            {
                yield return type;
            }
        }
    }

    private static IEnumerable<Assembly> GetModuleAssemblies(IServiceCollection services)
    {
        HashSet<Assembly> assemblies = [];
        IModuleContainer? container = services.GetSingletonInstanceOrNull<IModuleContainer>();
        if (container is not null)
        {
            foreach (SnowModuleDescriptor module in container.Modules)
            {
                assemblies.Add(module.Type.Assembly);
            }
        }

        Assembly? entryAssembly = Assembly.GetEntryAssembly();
        if (entryAssembly is not null)
        {
            assemblies.Add(entryAssembly);
        }

        return assemblies;
    }
}

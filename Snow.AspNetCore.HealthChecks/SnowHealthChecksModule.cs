using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Snow.DependencyInjection;
using Snow.Modularity;

namespace Snow.AspNetCore.HealthChecks;

/// <summary>
/// 健康检查模块: 把登记表里的检查项挂到框架的健康检查服务上
/// <para>依赖 <see cref="SnowAspNetCoreModule"/>: 健康端点是一个 HTTP 端点。</para>
/// <para>端点由模块声明, 框架在全部模块初始化完成之后统一映射, 宿主一行不用写。</para>
/// </summary>
[DependsOn(typeof(SnowAspNetCoreModule))]
public sealed class SnowHealthChecksModule : SnowModule
{
    /// <inheritdoc />
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        Configure<SnowHealthChecksOptions>(context.Services.GetConfiguration().GetSection("HealthChecks"));

        IHealthChecksBuilder builder = context.Services.AddHealthChecks();

        SnowHealthCheckRegistry? registry = SnowHealthChecksServiceCollectionExtensions.GetOrCreateRegistry(context.Services);
        foreach (SnowHealthCheckRegistration item in registry.Items)
        {
            builder.Add(new HealthCheckRegistration(
                item.Name,
                serviceProvider => (IHealthCheck)ActivatorUtilities.GetServiceOrCreateInstance(serviceProvider, item.CheckType),
                failureStatus: null,
                tags: item.Tags));
        }

        Configure<SnowEndpointRouterOptions>(options =>
            options.EndpointConfigureActions.Add(endpointContext => endpointContext.Endpoints.MapSnowHealthChecks()));
    }
}

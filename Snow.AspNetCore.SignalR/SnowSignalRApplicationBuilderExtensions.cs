using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Snow.AspNetCore.SignalR;

/// <summary>
/// Hub 端点挂载
/// </summary>
public static class SnowSignalRApplicationBuilderExtensions
{
    /// <summary>
    /// 把登记表里的 Hub 全部映射成端点
    /// <para>正常情况不用调: <c>SnowSignalRModule</c> 已把映射动作声明进端点清单,
    /// 框架在全部模块初始化完成之后统一执行。保留给单独使用本包的宿主。</para>
    /// </summary>
    /// <param name="endpoints">端点路由构建器</param>
    /// <returns>端点路由构建器本身</returns>
    public static IEndpointRouteBuilder MapSnowHubs(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        SnowSignalROptions options = endpoints.ServiceProvider.GetRequiredService<IOptions<SnowSignalROptions>>().Value;
        if (!options.Enabled)
        {
            return endpoints;
        }

        SnowHubRegistry registry = endpoints.ServiceProvider.GetRequiredService<SnowHubRegistry>();
        MapHubs(endpoints, registry);

        return endpoints;
    }

    /// <summary>
    /// 按登记表把 Hub 映射成端点
    /// </summary>
    /// <param name="endpoints">端点路由构建器</param>
    /// <param name="registry">Hub 登记表</param>
    internal static void MapHubs(IEndpointRouteBuilder endpoints, SnowHubRegistry registry)
    {
        MethodInfo mapHub = typeof(HubEndpointRouteBuilderExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(method =>
                method.Name == nameof(HubEndpointRouteBuilderExtensions.MapHub)
                && method.IsGenericMethodDefinition
                && method.GetParameters().Length == 2
                && method.GetParameters()[1].ParameterType == typeof(string));

        foreach (SnowHubRegistration item in registry.Items)
        {
            mapHub.MakeGenericMethod(item.HubType).Invoke(null, [endpoints, item.Path]);
        }
    }
}

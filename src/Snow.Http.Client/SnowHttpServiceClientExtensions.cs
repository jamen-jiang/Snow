using Microsoft.Extensions.DependencyInjection;

namespace Snow.Http.Client;

/// <summary>
/// 声明式 HTTP 接口的登记扩展: 登记后注入契约接口拿到的就是代理, 方法调用被翻译成 HTTP 请求
/// </summary>
public static class SnowHttpServiceClientExtensions
{
    /// <summary>
    /// 登记一个声明式 HTTP 契约(接口)
    /// </summary>
    /// <typeparam name="T">契约接口</typeparam>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合, 便于链式调用</returns>
    public static IServiceCollection AddSnowHttpServiceClient<T>(this IServiceCollection services) where T : class
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddSnowHttpServiceClients(typeof(T));
    }

    /// <summary>
    /// 批量登记声明式 HTTP 契约(接口)
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="contractTypes">契约接口类型</param>
    /// <returns>服务集合, 便于链式调用</returns>
    public static IServiceCollection AddSnowHttpServiceClients(this IServiceCollection services, params Type[] contractTypes)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(contractTypes);

        foreach (Type contractType in contractTypes)
        {
            if (!contractType.IsInterface)
            {
                throw new ArgumentException("声明式 HTTP 契约必须是接口: " + contractType.FullName, nameof(contractTypes));
            }

            Type captured = contractType;
            services.AddTransient(captured, serviceProvider => SnowHttpServiceProxyFactory.Create(captured, serviceProvider));
        }

        return services;
    }
}

using Microsoft.Extensions.DependencyInjection;

namespace Snow.Http.Client;

/// <summary>
/// 从容器直接取泛型客户端的扩展(不便于构造注入时用, 用完请释放)
/// </summary>
public static class SnowHttpClientProviderExtensions
{
    /// <summary>
    /// 取 <typeparamref name="T"/> 对应的客户端访问器
    /// </summary>
    /// <typeparam name="T">客户端契约(接口)类型</typeparam>
    /// <param name="serviceProvider">服务提供器</param>
    /// <returns>客户端访问器</returns>
    public static ISnowHttpClient<T> CreateSnowHttpClient<T>(this IServiceProvider serviceProvider) where T : class
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        return (ISnowHttpClient<T>)serviceProvider.GetRequiredService(typeof(ISnowHttpClient<>).MakeGenericType(typeof(T)));
    }
}

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Snow.DependencyInjection;

namespace Snow.AspNetCore;

/// <summary>
/// 服务提供者访问器的扩展(对应 ABP 的 <c>ServiceProviderAccessorExtensions</c>)
/// <para>初始化阶段拿不到 <c>HttpContext</c> 的参数(它不是构造期注入的), 只能从访问器里现取;
/// 放在 Snow.AspNetCore 是因为 <see cref="HttpContext"/> 属于 Web 框架, 与 ABP 把它放在 Volo.Abp.AspNetCore 同理。</para>
/// </summary>
public static class ServiceProviderAccessorExtensions
{
    /// <summary>
    /// 取当前请求: 不在请求线程上时返回 null
    /// </summary>
    /// <param name="serviceProviderAccessor">服务提供者访问器</param>
    /// <returns>当前请求或 null</returns>
    public static HttpContext? GetHttpContext(this IServiceProviderAccessor serviceProviderAccessor)
    {
        ArgumentNullException.ThrowIfNull(serviceProviderAccessor);

        return serviceProviderAccessor.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext;
    }
}

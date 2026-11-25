using Microsoft.AspNetCore.Hosting;
using Snow;
using Snow.DependencyInjection;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Web 宿主的服务登记扩展(对应 ABP 的 <c>AbpAspNetCoreServiceCollectionExtensions</c>,
/// 同在 <c>Microsoft.Extensions.DependencyInjection</c> 命名空间, 宿主不必额外 using)
/// </summary>
public static class SnowAspNetCoreServiceCollectionExtensions
{
    /// <summary>
    /// 取宿主的 Web 环境; 服务集合里没有(非 Web 宿主, 或还没被 Web 主机登记)时返回一个空实现, 环境名按 Development
    /// <para>与 ABP 的 <c>GetHostingEnvironment()</c> 一致: 调用方总能拿到非 null 的环境对象,
    /// 不必自己写 <c>??</c> 分支。空实现只用于读环境名/路径, 不能当真实环境用。</para>
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>宿主环境; 没有登记时是环境名为 Development 的空实现</returns>
    public static IWebHostEnvironment GetHostingEnvironment(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        IWebHostEnvironment? environment = services.GetSingletonInstanceOrNull<IWebHostEnvironment>();
        return environment ?? new EmptyHostingEnvironment { EnvironmentName = SnowEnvironments.Development };
    }
}

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Snow;
using Snow.AspNetCore.Security.Claims;
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

    /// <summary>
    /// 把外部令牌里的标准声明名(sub / role / email / name / family_name / given_name)映射成本框架的声明名
    /// <para>对应 ABP 的 <c>TransformAbpClaims()</c>: 登记一个 <see cref="IClaimsTransformation"/>,
    /// 认证中间件在认证完成后调用它, 于是 <c>context.User</c> 上同时有原始声明与映射后的声明,
    /// <see cref="Snow.Security.Claims.SnowClaimTypes"/> 那套读取口径(OIDC / 第三方 IdP 签发的令牌)因此能直接用。</para>
    /// <para>只登记不启用任何中间件, 也不改已有声明; 不需要映射就不要调。</para>
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合本身, 便于链式调用</returns>
    public static IServiceCollection TransformSnowClaims(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddTransient<IClaimsTransformation, SnowClaimsTransformation>();
    }
}

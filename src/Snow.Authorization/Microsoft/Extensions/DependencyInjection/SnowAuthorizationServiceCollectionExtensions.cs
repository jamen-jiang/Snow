using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Snow.Authorization;
using Snow.Authorization.Permissions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 授权的服务登记入口
/// </summary>
public static class SnowAuthorizationServiceCollectionExtensions
{
    /// <summary>
    /// 换掉权限校验器(默认是读声明的 <see cref="ClaimsPermissionChecker"/>)
    /// </summary>
    /// <typeparam name="TChecker">校验器实现类型</typeparam>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合本身, 便于链式调用</returns>
    public static IServiceCollection AddSnowPermissionChecker<TChecker>(this IServiceCollection services)
        where TChecker : class, IPermissionChecker
    {
        ArgumentNullException.ThrowIfNull(services);

        services.Replace(ServiceDescriptor.Transient<IPermissionChecker, TChecker>());

        return services;
    }
}

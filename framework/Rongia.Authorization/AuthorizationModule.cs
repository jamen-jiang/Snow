using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Rongia.Authorization.Permissions;
using Rongia.Modularity;
using Rongia.Security;

namespace Rongia.Authorization;

/// <summary>
/// 授权模块: 注册权限定义/值提供器管线, 并用动态策略提供器接管 ASP.NET Core 授权
/// </summary>
[DependsOn(typeof(SecurityModule))]
public sealed class AuthorizationModule : Module
{
    /// <inheritdoc />
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.Services.AddAuthorization();

        // 用动态策略提供器替换默认实现: 策略名可以就是权限名, 也可以带 permission: 前缀
        context.Services.Replace(ServiceDescriptor.Singleton<IAuthorizationPolicyProvider, RongiaAuthorizationPolicyProvider>());
        context.Services.AddScoped<IAuthorizationHandler, PermissionRequirementHandler>();

        context.Services.Configure<PermissionOptions>(options =>
        {
            options.ValueProviders.Add<SuperAdminPermissionValueProvider>();
            options.ValueProviders.Add<ClaimsPermissionValueProvider>();
            options.ValueProviders.Add<UserPermissionValueProvider>();
            options.ValueProviders.Add<RolePermissionValueProvider>();
        });
    }
}

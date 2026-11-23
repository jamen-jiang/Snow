using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Snow.Authorization.Permissions;
using Snow.DependencyInjection;
using Snow.Modularity;
using Snow.Security;

namespace Snow.Authorization;

/// <summary>
/// 授权模块: 权限定义树 + 动态策略 + 校验器
/// <para>权限定义提供器按基类 <see cref="PermissionDefinitionProvider"/> 扫描登记 —— 约定注册只按接口或自身类型登记,
/// 不会登记抽象基类的实现, 所以这里补一遍。</para>
/// <para>判定链路: 注解 <c>[Authorize("权限名")]</c> → <see cref="SnowAuthorizationPolicyProvider"/> →
/// <see cref="PermissionRequirementHandler"/> → <see cref="IPermissionChecker"/> → 身份里的 permission 声明。</para>
/// <para>框架不保存"谁被授予了什么": 授予关系在签发令牌时写成权限声明, 天然没有跨实例同步与重启丢失的问题。</para>
/// </summary>
[DependsOn(typeof(SnowSecurityModule))]
public sealed class SnowAuthorizationModule : SnowModule
{
    /// <inheritdoc />
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.Services.AddAuthorization();

        // 动态策略提供器(SnowAuthorizationPolicyProvider 标了 [SingletonService(ReplaceServices = true)])
        // 由约定注册顶掉框架自带的 DefaultAuthorizationPolicyProvider, 这里不用再登记。

        IModuleContainer container = context.Services.GetSingletonInstance<IModuleContainer>();

        foreach (Type providerType in container.Modules
                     .SelectMany(module => module.AllAssemblies)
                     .Distinct()
                     .SelectMany(assembly => assembly.GetExportedTypes())
                     .Where(type => type is { IsClass: true, IsAbstract: false }
                                    && typeof(PermissionDefinitionProvider).IsAssignableFrom(type)))
        {
            context.Services.TryAddEnumerable(ServiceDescriptor.Singleton(typeof(PermissionDefinitionProvider), providerType));
        }
    }
}

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

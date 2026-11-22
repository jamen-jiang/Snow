using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Snow.Authorization.Permissions;
using Snow.DependencyInjection;
using Snow.Modularity;
using Snow.Security;

namespace Snow.Authorization;

/// <summary>
/// 授权模块: 注册权限定义/值提供器管线, 并用动态策略提供器接管 ASP.NET Core 授权
/// <para>权限定义提供器与权限值提供器都按类型自动扫描登记: 只要实现 <see cref="IPermissionDefinitionProvider"/>
/// 或 <see cref="IPermissionValueProvider"/>(通常是继承 <see cref="PermissionDefinitionProvider"/> 或
/// <see cref="PermissionValueProvider"/>), 就会被打包进模块清单的程序集里找出来, 不需要再写
/// <c>Configure&lt;PermissionOptions&gt;(...)</c> 手动 Add。</para>
/// </summary>
[DependsOn(typeof(SnowSecurityModule))]
public sealed class SnowAuthorizationModule : SnowModule
{
    /// <inheritdoc />
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.Services.AddAuthorization();

        // 动态策略提供器: 策略名可以就是权限名, 也可以带 permission: 前缀
        // SnowAuthorizationPolicyProvider 上标了 [SingletonService(ReplaceServices = true)], 约定注册会顶掉
        // 框架 AddAuthorization 自带的 DefaultAuthorizationPolicyProvider; 装了 Snow.Authentication 时
        // TokenPolicyProvider 会再顶一次(它在 Authorization 之后加载), 最终由它对外提供策略。

        Type[] definitionProviders = FindImplementations(context.Services, typeof(IPermissionDefinitionProvider));
        Type[] valueProviders = FindImplementations(context.Services, typeof(IPermissionValueProvider));

        // 扫描到的实现自己登记一遍: 只实现契约、没打生命周期标记的类型也能解析出来
        foreach (Type providerType in definitionProviders.Concat(valueProviders))
        {
            context.Services.TryAddTransient(providerType);
        }

        Configure<PermissionOptions>(options =>
        {
            // 定义提供器顺序无关: 各模块各自往自己的分组里加权限, 重名会被 PermissionDefinitionManager 拦下
            foreach (Type providerType in definitionProviders)
            {
                if (!options.DefinitionProviders.Contains(providerType))
                {
                    options.DefinitionProviders.Add(providerType);
                }
            }

            // 值提供器: 内置两个先登记(顺序固定, 便于阅读), 扫描到的自定义实现按全名追加
            options.ValueProviders.Add<UserPermissionValueProvider>();
            options.ValueProviders.Add<RolePermissionValueProvider>();

            foreach (Type providerType in valueProviders)
            {
                if (!options.ValueProviders.Contains(providerType))
                {
                    options.ValueProviders.Add(providerType);
                }
            }
        });
    }

    /// <summary>
    /// 在模块清单的全部程序集里找出契约的实现类型, 按全名排序保证每次启动顺序一致
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="contract">契约类型</param>
    /// <returns>实现类型数组</returns>
    private static Type[] FindImplementations(IServiceCollection services, Type contract)
    {
        IModuleContainer container = services.GetSingletonInstance<IModuleContainer>();

        Assembly[] assemblies = [.. container.Modules
            .SelectMany(module => module.AllAssemblies)
            .Distinct()];

        return [.. assemblies
            .SelectMany(AssemblyTypeCache.GetTypes)
            .Where(type => type is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false }
                && contract.IsAssignableFrom(type))
            .Distinct()
            .OrderBy(type => type.FullName, StringComparer.Ordinal)];
    }
}

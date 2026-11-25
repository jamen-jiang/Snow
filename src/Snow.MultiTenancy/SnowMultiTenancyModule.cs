using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Snow.Modularity;
using Snow.Security;

namespace Snow.MultiTenancy;

/// <summary>
/// 多租户模块(对应 ABP 的 <c>AbpMultiTenancyModule</c>)
/// <para>当前租户上下文 / 租户信息容器 / 租户存储 / 解析器靠生命周期注解参与约定注册;
/// 这里把根配置里的租户清单绑进默认租户存储、把选项暴露成 <see cref="IMultiTenancyConfig"/>,
/// 并把当前用户出资者排到出资者最前面。</para>
/// <para>开关不读配置(与 ABP 一致): 由宿主在模块里
/// <c>Configure&lt;SnowMultiTenancyOptions&gt;(options => options.IsEnabled = true)</c> 打开。</para>
/// </summary>
[DependsOn(typeof(SnowSecurityModule))]
public sealed class SnowMultiTenancyModule : SnowModule
{
    /// <inheritdoc />
    /// <param name="context">上下文</param>
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // 整体绑定根配置(与 ABP 相同): 租户清单放在根的 Tenants 数组里, 不嗅探配置节是否存在
        Configure<SnowDefaultTenantStoreOptions>(context.Configuration);

        context.Services.TryAddSingleton<IMultiTenancyConfig>(serviceProvider =>
            serviceProvider.GetRequiredService<IOptions<SnowMultiTenancyOptions>>().Value);

        // 出资者顺序即优先级: 当前用户最先, Web 层那四个由 Snow.AspNetCore.MultiTenancy 追加在后
        context.Services.TryAddEnumerable(ServiceDescriptor.Transient<ITenantResolveContributor, CurrentUserTenantResolveContributor>());
    }
}

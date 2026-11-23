using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Snow.Modularity;
using Snow.Security;
using Snow.Settings;

namespace Snow.MultiTenancy;

/// <summary>
/// 多租户模块(对应 ABP 的 <c>AbpMultiTenancyModule</c>)
/// <para>当前租户上下文 / 租户信息容器 / 租户存储 / 解析器 / 租户设置提供器靠生命周期注解参与约定注册;
/// 这里绑定配置节、把选项暴露成 <see cref="IMultiTenancyConfig"/>, 并把当前用户出资者排到出资者最前面。</para>
/// <para>依赖 <see cref="SnowSettingsModule"/>: 租户级设置(<see cref="TenantSettingValueProvider"/>)要写进设置体系, 与 ABP 一致。</para>
/// </summary>
[DependsOn(typeof(SnowSecurityModule), typeof(SnowSettingsModule))]
public sealed class SnowMultiTenancyModule : SnowModule
{
    /// <inheritdoc />
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Configuration.GetSection("MultiTenancy").Exists())
        {
            Configure<SnowMultiTenancyOptions>(context.Configuration.GetSection("MultiTenancy"));
        }

        if (context.Configuration.GetSection("MultiTenancy:Tenants").Exists())
        {
            Configure<SnowDefaultTenantStoreOptions>(context.Configuration.GetSection("MultiTenancy"));
        }

        // 租户设置排在全局设置之后 => 读取时反过来遍历, 租户设置压过全局 (与 ABP 的 InsertAfter(Global, Tenant) 同义)
        Configure<SnowSettingOptions>(options =>
        {
            int index = options.ValueProviders.FindIndex(type => type == typeof(GlobalSettingValueProvider));
            options.ValueProviders.Insert(index < 0 ? options.ValueProviders.Count : index + 1, typeof(TenantSettingValueProvider));
        });

        context.Services.TryAddSingleton<IMultiTenancyConfig>(serviceProvider =>
            serviceProvider.GetRequiredService<IOptions<SnowMultiTenancyOptions>>().Value);

        // 出资者顺序即优先级: 当前用户最先, Web 层那四个由 Snow.AspNetCore.MultiTenancy 追加在后
        context.Services.TryAddEnumerable(ServiceDescriptor.Transient<ITenantResolveContributor, CurrentUserTenantResolveContributor>());
    }
}

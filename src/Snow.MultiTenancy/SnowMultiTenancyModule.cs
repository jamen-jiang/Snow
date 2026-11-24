using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Snow.Modularity;
using Snow.Security;

namespace Snow.MultiTenancy;

/// <summary>
/// 多租户模块(对应 ABP 的 <c>AbpMultiTenancyModule</c>)
/// <para>当前租户上下文 / 租户信息容器 / 租户存储 / 解析器靠生命周期注解参与约定注册;
/// 这里绑定配置节、把选项暴露成 <see cref="IMultiTenancyConfig"/>, 并把当前用户出资者排到出资者最前面。</para>
/// </summary>
[DependsOn(typeof(SnowSecurityModule))]
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

        context.Services.TryAddSingleton<IMultiTenancyConfig>(serviceProvider =>
            serviceProvider.GetRequiredService<IOptions<SnowMultiTenancyOptions>>().Value);

        // 出资者顺序即优先级: 当前用户最先, Web 层那四个由 Snow.AspNetCore.MultiTenancy 追加在后
        context.Services.TryAddEnumerable(ServiceDescriptor.Transient<ITenantResolveContributor, CurrentUserTenantResolveContributor>());
    }
}

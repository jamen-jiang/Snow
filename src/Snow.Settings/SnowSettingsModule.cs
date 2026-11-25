using Snow.Modularity;
using Snow.Security;

namespace Snow.Settings;

/// <summary>
/// 配置项模块(对应 ABP 的 AbpSettingsModule): 只登记默认取值来源, 定义提供器由各能力包自己往上加
/// </summary>
[DependsOn(typeof(SnowSecurityModule))]
public sealed class SnowSettingsModule : SnowModule
{
    /// <inheritdoc />
    /// <param name="context">上下文</param>
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        Configure<SnowSettingOptions>(options =>
        {
            // 默认来源: appsettings.json 的 Settings: 前缀
            options.ValueProviders.Add<ConfigurationSettingValueProvider>();
        });
    }
}

using Microsoft.Extensions.DependencyInjection;
using Snow.Modularity;
using Snow.Settings;

namespace Snow.Emailing;

/// <summary>
/// 邮件模块: 把邮件配置项(<see cref="EmailSettingNames"/>)挂到配置项系统, 值由宿主在 <c>Settings</c> 节或其它来源提供
/// <para>本模块只带静默实现(<see cref="NullEmailSender"/>), 真正投递需再引入 Snow.Emailing.MailKit 顶替它。</para>
/// </summary>
[DependsOn(typeof(SnowSettingsModule))]
public sealed class SnowEmailingModule : SnowModule
{
    /// <inheritdoc />
    /// <param name="context">上下文</param>
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        Configure<SnowSettingOptions>(options => options.DefinitionProviders.Add<EmailSettingProvider>());

        // EmailOptions 只剩 Snow 自己的两个开关(超时/正文默认格式), 不绑配置节
        context.Services.AddOptions<EmailOptions>();
    }
}

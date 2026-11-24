using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Snow.DependencyInjection;
using Snow.Modularity;

namespace Snow.Emailing;

/// <summary>
/// 邮件模块: 绑定 <c>Email</c> 配置节到 <see cref="EmailOptions"/>
/// <para>本模块只带静默实现(<see cref="NullEmailSender"/>), 真正投递需再引入 Snow.Emailing.MailKit 顶替它。</para>
/// </summary>
public sealed class SnowEmailingModule : SnowModule
{
    /// <inheritdoc />
    /// <param name="context">上下文</param>
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // 配置节: Email:{ SmtpHost, Port, UserName, Password, DefaultFromAddress, DefaultFromDisplayName, UseSsl }
        IConfiguration configuration = context.Services.GetConfiguration();
        Configure<EmailOptions>(configuration.GetSection("Email"));
    }
}

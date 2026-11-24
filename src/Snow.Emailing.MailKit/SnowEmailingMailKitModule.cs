using Microsoft.Extensions.DependencyInjection;
using Snow.Modularity;

namespace Snow.Emailing.MailKit;

/// <summary>
/// 邮件 MailKit 模块: 依赖 <see cref="SnowEmailingModule"/>, 用 <see cref="MailKitEmailSender"/> 顶替契约包里的静默实现
/// <para>顶替靠 <see cref="MailKitEmailSender"/> 上的 <c>[TransientService(ReplaceServices = true)]</c> 由约定注册完成, 这里不需要写替换代码。</para>
/// <para>配置只有 <c>Email</c> 一节(见 <see cref="EmailOptions"/>), 本实现不额外引入自己的配置节。</para>
/// </summary>
[DependsOn(typeof(SnowEmailingModule))]
public sealed class SnowEmailingMailKitModule : SnowModule
{
    /// <inheritdoc />
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // 配置体检放在这里(启动时一次性校验), 不塞进 SendAsync:
        // 缺 SMTP 地址或发件人属于部署配置错误, 应当在启动时明确报出来, 而不是等某次发信才炸。
        context.Services.AddOptions<EmailOptions>()
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.SmtpHost),
                "未配置 SMTP 服务器地址: 请设置 Email:SmtpHost。")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.DefaultFromAddress),
                "未配置发件人地址: 请设置 Email:DefaultFromAddress。")
            .ValidateOnStart();
    }
}

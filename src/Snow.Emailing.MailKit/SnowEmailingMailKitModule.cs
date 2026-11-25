using Snow.Modularity;

namespace Snow.Emailing.MailKit;

/// <summary>
/// 邮件 MailKit 模块: 依赖 <see cref="SnowEmailingModule"/>, 用 <see cref="MailKitEmailSender"/> 顶替契约包里的静默实现
/// <para>顶替靠 <see cref="MailKitEmailSender"/> 上的 <c>[TransientService(ReplaceServices = true)]</c> 由约定注册完成, 这里不需要写替换代码。</para>
/// <para>SMTP 参数在配置项系统里(见 <see cref="EmailSettingNames"/>), 缺配置会在真正发信时报出来, 与 ABP 一致。</para>
/// </summary>
[DependsOn(typeof(SnowEmailingModule))]
public sealed class SnowEmailingMailKitModule : SnowModule
{
}

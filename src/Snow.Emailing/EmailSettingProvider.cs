using Snow.Settings;

namespace Snow.Emailing;

/// <summary>
/// 声明 9 个邮件配置项(对应 ABP 的 EmailSettingProvider)
/// <para>密码项标记为加密: 配置里应放 <see cref="Snow.Security.Encryption.IStringEncryptionService"/> 产出的密文,
/// 读取时自动解密。</para>
/// </summary>
public class EmailSettingProvider : SettingDefinitionProvider
{
    /// <inheritdoc />
    /// <param name="context">登记上下文</param>
    public override void Define(ISettingDefinitionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.Add(
            new SettingDefinition(EmailSettingNames.Smtp.Host, "127.0.0.1", "SMTP 服务器地址"),
            new SettingDefinition(EmailSettingNames.Smtp.Port, "25", "SMTP 端口"),
            new SettingDefinition(EmailSettingNames.Smtp.UserName, null, "SMTP 登录账号"),
            new SettingDefinition(EmailSettingNames.Smtp.Password, null, "SMTP 登录密码", isEncrypted: true),
            new SettingDefinition(EmailSettingNames.Smtp.Domain, null, "SMTP 认证域"),
            new SettingDefinition(EmailSettingNames.Smtp.EnableSsl, "false", "是否使用 SSL 直连"),
            new SettingDefinition(EmailSettingNames.Smtp.UseDefaultCredentials, "true", "是否使用默认凭据"),
            new SettingDefinition(EmailSettingNames.DefaultFromAddress, "noreply@snow.io", "默认发件人地址"),
            new SettingDefinition(EmailSettingNames.DefaultFromDisplayName, "Snow application", "默认发件人显示名"));
    }
}

using Snow.Settings;

namespace Snow.Emailing;

/// <summary>
/// 声明 9 个邮件配置项
/// <para>密码项标记为加密: 配置里应放 <see cref="Snow.Security.Encryption.IStringEncryptionService"/> 产出的密文,
/// 读取时自动解密。</para>
/// </summary>
public class EmailSettingProvider : SettingDefinitionProvider
{
    /// <inheritdoc />
    /// <param name="context">登记上下文</param>
    public override void Define(SettingDefinitionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.Add(
            new SettingDefinition(EmailSettingNames.Smtp.Host, "127.0.0.1"),
            new SettingDefinition(EmailSettingNames.Smtp.Port, "25"),
            new SettingDefinition(EmailSettingNames.Smtp.UserName),
            new SettingDefinition(EmailSettingNames.Smtp.Password, isEncrypted: true),
            new SettingDefinition(EmailSettingNames.Smtp.Domain),
            new SettingDefinition(EmailSettingNames.Smtp.EnableSsl, "false"),
            new SettingDefinition(EmailSettingNames.Smtp.UseDefaultCredentials, "true"),
            new SettingDefinition(EmailSettingNames.DefaultFromAddress, "noreply@snow.io"),
            new SettingDefinition(EmailSettingNames.DefaultFromDisplayName, "Snow application"));
    }
}

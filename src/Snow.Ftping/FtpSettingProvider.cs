using Snow.Settings;

namespace Snow.Ftping;

/// <summary>
/// 声明 10 个 FTP 配置项
/// <para>密码项标记为加密: 配置里应放 <see cref="Snow.Security.Encryption.IStringEncryptionService"/> 产出的密文,
/// 读取时自动解密。</para>
/// </summary>
public class FtpSettingProvider : SettingDefinitionProvider
{
    /// <inheritdoc />
    /// <param name="context">登记上下文</param>
    public override void Define(SettingDefinitionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.Add(
            new SettingDefinition(FtpSettingNames.Host),
            new SettingDefinition(FtpSettingNames.Port, "21"),
            new SettingDefinition(FtpSettingNames.UserName),
            new SettingDefinition(FtpSettingNames.Password, isEncrypted: true),
            new SettingDefinition(FtpSettingNames.UseSsl, "false"),
            new SettingDefinition(FtpSettingNames.EncodingName, "utf-8"),
            new SettingDefinition(FtpSettingNames.ConnectTimeout, "10000"),
            new SettingDefinition(FtpSettingNames.ReadTimeout, "30000"),
            new SettingDefinition(FtpSettingNames.IgnoreCertificateErrors, "false"),
            new SettingDefinition(FtpSettingNames.RetryAttempts, "3"));
    }
}

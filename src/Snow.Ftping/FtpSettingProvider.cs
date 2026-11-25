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
    public override void Define(ISettingDefinitionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.Add(
            new SettingDefinition(FtpSettingNames.Host, null, "FTP 服务器地址"),
            new SettingDefinition(FtpSettingNames.Port, "21", "FTP 端口"),
            new SettingDefinition(FtpSettingNames.UserName, null, "FTP 登录账号"),
            new SettingDefinition(FtpSettingNames.Password, null, "FTP 登录密码", isEncrypted: true),
            new SettingDefinition(FtpSettingNames.UseSsl, "false", "是否使用 SSL/TLS 加密连接"),
            new SettingDefinition(FtpSettingNames.EncodingName, "utf-8", "文件名编码"),
            new SettingDefinition(FtpSettingNames.ConnectTimeout, "10000", "连接超时(毫秒)"),
            new SettingDefinition(FtpSettingNames.ReadTimeout, "30000", "读写超时(毫秒)"),
            new SettingDefinition(FtpSettingNames.IgnoreCertificateErrors, "false", "是否忽略证书校验"),
            new SettingDefinition(FtpSettingNames.RetryAttempts, "3", "失败重试次数"));
    }
}

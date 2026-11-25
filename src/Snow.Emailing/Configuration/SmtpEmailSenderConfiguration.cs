using Snow.DependencyInjection;
using Snow.Exceptions;
using Snow.Settings;

namespace Snow.Emailing.Configuration;

/// <summary>
/// <see cref="ISmtpEmailSenderConfiguration"/> 的默认实现(对应 ABP 的 SmtpEmailSenderConfiguration):
/// 全部值来自配置项系统
/// </summary>
[TransientService]
public class SmtpEmailSenderConfiguration : EmailSenderConfiguration, ISmtpEmailSenderConfiguration
{
    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="settingProvider">配置项读取入口</param>
    public SmtpEmailSenderConfiguration(ISettingProvider settingProvider)
        : base(settingProvider)
    {
    }

    /// <inheritdoc />
    public Task<string> GetHostAsync()
    {
        return GetNotEmptySettingValueAsync(EmailSettingNames.Smtp.Host);
    }

    /// <inheritdoc />
    public async Task<int> GetPortAsync()
    {
        string value = await GetNotEmptySettingValueAsync(EmailSettingNames.Smtp.Port).ConfigureAwait(false);

        return int.TryParse(value, out int port)
            ? port
            : throw new SnowException("配置项 " + EmailSettingNames.Smtp.Port + " 的值不是合法端口: " + value);
    }

    /// <inheritdoc />
    public Task<string> GetUserNameAsync()
    {
        return GetNotEmptySettingValueAsync(EmailSettingNames.Smtp.UserName);
    }

    /// <inheritdoc />
    public Task<string> GetPasswordAsync()
    {
        return GetNotEmptySettingValueAsync(EmailSettingNames.Smtp.Password);
    }

    /// <inheritdoc />
    public Task<string?> GetDomainAsync()
    {
        return SettingProvider.GetOrNullAsync(EmailSettingNames.Smtp.Domain);
    }

    /// <inheritdoc />
    public async Task<bool> GetEnableSslAsync()
    {
        return await GetBooleanSettingValueAsync(EmailSettingNames.Smtp.EnableSsl).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> GetUseDefaultCredentialsAsync()
    {
        return await GetBooleanSettingValueAsync(EmailSettingNames.Smtp.UseDefaultCredentials).ConfigureAwait(false);
    }

    private async Task<bool> GetBooleanSettingValueAsync(string name)
    {
        string value = await GetNotEmptySettingValueAsync(name).ConfigureAwait(false);

        return bool.TryParse(value, out bool result)
            ? result
            : throw new SnowException("配置项 " + name + " 的值不是合法布尔值: " + value);
    }
}

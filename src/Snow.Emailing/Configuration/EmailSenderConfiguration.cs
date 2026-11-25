using Snow.DependencyInjection;
using Snow.Exceptions;
using Snow.Settings;

namespace Snow.Emailing.Configuration;

/// <summary>
/// <see cref="IEmailSenderConfiguration"/> 的默认实现(对应 ABP 的 EmailSenderConfiguration):
/// 值来自 <see cref="ISettingProvider"/>, 空值直接抛异常
/// </summary>
[TransientService]
public class EmailSenderConfiguration : IEmailSenderConfiguration
{
    /// <summary>
    /// 配置项读取入口
    /// </summary>
    protected ISettingProvider SettingProvider { get; }

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="settingProvider">配置项读取入口</param>
    public EmailSenderConfiguration(ISettingProvider settingProvider)
    {
        ArgumentNullException.ThrowIfNull(settingProvider);

        SettingProvider = settingProvider;
    }

    /// <inheritdoc />
    public Task<string> GetDefaultFromAddressAsync()
    {
        return GetNotEmptySettingValueAsync(EmailSettingNames.DefaultFromAddress);
    }

    /// <inheritdoc />
    public Task<string> GetDefaultFromDisplayNameAsync()
    {
        return GetNotEmptySettingValueAsync(EmailSettingNames.DefaultFromDisplayName);
    }

    /// <summary>
    /// 取一个必须有值的配置项
    /// </summary>
    /// <param name="name">配置项名</param>
    /// <returns>值</returns>
    /// <exception cref="SnowException">值为空</exception>
    protected virtual async Task<string> GetNotEmptySettingValueAsync(string name)
    {
        string? value = await SettingProvider.GetOrNullAsync(name).ConfigureAwait(false);
        if (value.IsNullOrEmpty())
        {
            throw new SnowException("配置项 " + name + " 的值不能为空。");
        }

        return value;
    }
}

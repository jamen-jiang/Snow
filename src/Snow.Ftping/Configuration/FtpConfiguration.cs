using Snow.DependencyInjection;
using Snow.Exceptions;
using Snow.Settings;

namespace Snow.Ftping.Configuration;

/// <summary>
/// <see cref="IFtpConfiguration"/> 的默认实现: 值来自 <see cref="ISettingProvider"/>, 必填项为空直接抛异常
/// </summary>
[TransientService]
public class FtpConfiguration : IFtpConfiguration
{
    /// <summary>
    /// 配置项读取入口
    /// </summary>
    protected ISettingProvider SettingProvider { get; }

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="settingProvider">配置项读取入口</param>
    public FtpConfiguration(ISettingProvider settingProvider)
    {
        ArgumentNullException.ThrowIfNull(settingProvider);

        SettingProvider = settingProvider;
    }

    /// <inheritdoc />
    public Task<string> GetHostAsync()
    {
        return GetNotEmptySettingValueAsync(FtpSettingNames.Host);
    }

    /// <inheritdoc />
    public Task<int> GetPortAsync()
    {
        return GetIntegerSettingValueAsync(FtpSettingNames.Port);
    }

    /// <inheritdoc />
    public Task<string?> GetUserNameAsync()
    {
        return SettingProvider.GetOrNullAsync(FtpSettingNames.UserName);
    }

    /// <inheritdoc />
    public Task<string?> GetPasswordAsync()
    {
        return SettingProvider.GetOrNullAsync(FtpSettingNames.Password);
    }

    /// <inheritdoc />
    public Task<bool> GetUseSslAsync()
    {
        return GetBooleanSettingValueAsync(FtpSettingNames.UseSsl);
    }

    /// <inheritdoc />
    public async Task<string> GetEncodingNameAsync()
    {
        return await SettingProvider.GetOrNullAsync(FtpSettingNames.EncodingName).ConfigureAwait(false) ?? string.Empty;
    }

    /// <inheritdoc />
    public Task<int> GetConnectTimeoutAsync()
    {
        return GetIntegerSettingValueAsync(FtpSettingNames.ConnectTimeout);
    }

    /// <inheritdoc />
    public Task<int> GetReadTimeoutAsync()
    {
        return GetIntegerSettingValueAsync(FtpSettingNames.ReadTimeout);
    }

    /// <inheritdoc />
    public Task<bool> GetIgnoreCertificateErrorsAsync()
    {
        return GetBooleanSettingValueAsync(FtpSettingNames.IgnoreCertificateErrors);
    }

    /// <inheritdoc />
    public Task<int> GetRetryAttemptsAsync()
    {
        return GetIntegerSettingValueAsync(FtpSettingNames.RetryAttempts);
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

    private async Task<int> GetIntegerSettingValueAsync(string name)
    {
        string value = await GetNotEmptySettingValueAsync(name).ConfigureAwait(false);

        return int.TryParse(value, out int result)
            ? result
            : throw new SnowException("配置项 " + name + " 的值不是合法整数: " + value);
    }

    private async Task<bool> GetBooleanSettingValueAsync(string name)
    {
        string value = await GetNotEmptySettingValueAsync(name).ConfigureAwait(false);

        return bool.TryParse(value, out bool result)
            ? result
            : throw new SnowException("配置项 " + name + " 的值不是合法布尔值: " + value);
    }
}

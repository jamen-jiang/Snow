using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.Configuration;

/// <summary>
/// <see cref="IConfigurationBuilder"/> 扩展(对应 ABP 的 <c>AbpConfigurationExtensions</c>)。
/// </summary>
public static class SnowConfigurationExtensions
{
    /// <summary>
    /// 追加 appsettings.secrets.json 到配置源。
    /// </summary>
    public static IConfigurationBuilder AddAppSettingsSecretsJson(
        this IConfigurationBuilder builder,
        bool optional = true,
        bool reloadOnChange = true,
        string path = SnowHostingHostBuilderExtensions.AppSettingsSecretJsonPath)
    {
        return builder.AddJsonFile(path: path, optional: optional, reloadOnChange: reloadOnChange);
    }
}

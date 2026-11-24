using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// <see cref="IHostBuilder"/> 扩展(对应 ABP 的 <c>AbpHostingHostBuilderExtensions</c>)。
/// </summary>
public static class SnowHostingHostBuilderExtensions
{
    /// <summary>
    /// 用户机密文件的默认路径。
    /// </summary>
    public const string AppSettingsSecretJsonPath = "appsettings.secrets.json";

    /// <summary>
    /// 把 appsettings.secrets.json 追加到宿主的配置构建流程。
    /// </summary>
    /// <param name="hostBuilder">宿主构建器</param>
    /// <param name="optional">是否可选</param>
    /// <param name="reloadOnChange">变更时是否重新加载</param>
    /// <param name="path">路径</param>
    public static IHostBuilder AddAppSettingsSecretsJson(
        this IHostBuilder hostBuilder,
        bool optional = true,
        bool reloadOnChange = true,
        string path = AppSettingsSecretJsonPath)
    {
        return hostBuilder.ConfigureAppConfiguration((_, builder) =>
        {
            builder.AddJsonFile(path: path, optional: optional, reloadOnChange: reloadOnChange);
        });
    }
}

using System;
using System.IO;

namespace Microsoft.Extensions.Configuration;

/// <summary>
/// 构建配置根对象的工具(对应 ABP 的 <c>ConfigurationHelper</c>)。
/// </summary>
public static class ConfigurationHelper
{
    /// <summary>
    /// 按约定把 appsettings.json / .secrets.json / 环境变量 / 命令行依次叠成一个配置根。
    /// </summary>
    /// <param name="options">配置构建选项</param>
    /// <param name="builderAction">对配置构建器追加定制</param>
    /// <returns>构建好的配置根</returns>
    public static IConfigurationRoot BuildConfiguration(
        SnowConfigurationBuilderOptions? options = null,
        Action<IConfigurationBuilder>? builderAction = null)
    {
        options ??= new SnowConfigurationBuilderOptions();

        if (options.BasePath.IsNullOrEmpty())
        {
            options.BasePath = Directory.GetCurrentDirectory();
        }

        var builder = new ConfigurationBuilder()
            .SetBasePath(options.BasePath!)
            .AddJsonFile(options.FileName + ".json", optional: options.Optional, reloadOnChange: options.ReloadOnChange)
            .AddJsonFile(options.FileName + ".secrets.json", optional: true, reloadOnChange: options.ReloadOnChange);

        if (!options.EnvironmentName.IsNullOrEmpty())
        {
            builder = builder.AddJsonFile($"{options.FileName}.{options.EnvironmentName}.json", optional: true, reloadOnChange: options.ReloadOnChange);
        }

        if (options.EnvironmentName == "Development")
        {
            if (options.UserSecretsId != null)
            {
                builder.AddUserSecrets(options.UserSecretsId);
            }
            else if (options.UserSecretsAssembly != null)
            {
                builder.AddUserSecrets(options.UserSecretsAssembly, true);
            }
        }

        builder = builder.AddEnvironmentVariables(options.EnvironmentVariablesPrefix);

        if (options.CommandLineArgs != null)
        {
            builder = builder.AddCommandLine(options.CommandLineArgs);
        }

        builderAction?.Invoke(builder);

        return builder.Build();
    }
}

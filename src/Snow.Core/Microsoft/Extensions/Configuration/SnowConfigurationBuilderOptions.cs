using System.Reflection;

namespace Microsoft.Extensions.Configuration;

/// <summary>
/// 构建配置对象时的选项(对应 ABP 的 <c>AbpConfigurationBuilderOptions</c>)。
/// </summary>
public class SnowConfigurationBuilderOptions
{
    /// <summary>
    /// 用来取用户机密所属的程序集; 与 <see cref="UserSecretsId"/> 二选一, 后者优先。
    /// </summary>
    public Assembly? UserSecretsAssembly { get; set; }

    /// <summary>
    /// 用户机密 Id; 优先于 <see cref="UserSecretsAssembly"/>。
    /// </summary>
    public string? UserSecretsId { get; set; }

    /// <summary>
    /// 配置文件名(不含扩展名), 默认 <c>appsettings</c>。
    /// </summary>
    public string FileName { get; set; } = "appsettings";

    /// <summary>
    /// 文件是否可选, 默认 true。
    /// </summary>
    public bool Optional { get; set; } = true;

    /// <summary>
    /// 文件变更时是否重新加载, 默认 true。
    /// </summary>
    public bool ReloadOnChange { get; set; } = true;

    /// <summary>
    /// 环境名, 通常是 Development / Staging / Production。
    /// </summary>
    public string? EnvironmentName { get; set; }

    /// <summary>
    /// 读取配置文件的基准目录。
    /// </summary>
    public string? BasePath { get; set; }

    /// <summary>
    /// 环境变量前缀。
    /// </summary>
    public string? EnvironmentVariablesPrefix { get; set; }

    /// <summary>
    /// 命令行参数。
    /// </summary>
    public string[]? CommandLineArgs { get; set; }
}

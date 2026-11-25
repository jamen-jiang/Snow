using Snow.Modularity;
using Snow.Settings;

namespace Snow.Ftping;

/// <summary>
/// FTP 模块: 把 FTP 配置项(<see cref="FtpSettingNames"/>)挂到配置项系统, 值由宿主在 <c>Settings</c> 节或其它来源提供
/// <para>本包只有契约(<see cref="IFtpClient"/> / <see cref="FtpEntry"/> / <see cref="FtpFileInfo"/>) 与兜底实现
/// <see cref="NullFtpClient"/>: 不配 FTP 也能启动, 但所有写操作都会记 warning 并如实返回失败,
/// 真投递需再引入 Snow.Ftping.FluentFTP(它带 ReplaceServices 顶替 <see cref="NullFtpClient"/>)。</para>
/// </summary>
[DependsOn(typeof(SnowSettingsModule))]
public sealed class SnowFtpingModule : SnowModule
{
    /// <inheritdoc />
    /// <param name="context">上下文</param>
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        Configure<SnowSettingOptions>(options => options.DefinitionProviders.Add<FtpSettingProvider>());
    }
}

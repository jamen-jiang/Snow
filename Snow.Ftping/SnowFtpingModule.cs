using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Snow.DependencyInjection;
using Snow.Modularity;

namespace Snow.Ftping;

/// <summary>
/// FTP 模块: 绑定 <c>Ftp</c> 配置节到 <see cref="FtpOptions"/>
/// <para>本包只有契约(<see cref="IFtpClient"/> / <see cref="FtpFile"/> / <see cref="FtpOptions"/>) 与兜底实现
/// <see cref="NullFtpClient"/>: 不配 FTP 也能启动, 但所有写操作都会记 warning 并如实返回失败,
/// 真投递需再引入 Snow.Ftping.FluentFTP(它带 ReplaceServices 顶替 <see cref="NullFtpClient"/>)。</para>
/// </summary>
public sealed class SnowFtpingModule : SnowModule
{
    /// <inheritdoc />
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // 配置节: Ftp:{ FtpHost, Port, UserName, Password, UseSsl, EncodingName, ConnectTimeout, ReadTimeout, IgnoreCertificateErrors, RetryAttempts }
        IConfiguration configuration = context.Services.GetConfiguration();
        Configure<FtpOptions>(configuration.GetSection("Ftp"));
    }
}

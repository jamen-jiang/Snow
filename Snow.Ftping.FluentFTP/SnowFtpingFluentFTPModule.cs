using System.Text;
using Snow.Modularity;

namespace Snow.Ftping.FluentFTP;

/// <summary>
/// FTP FluentFTP 模块: 依赖 <see cref="SnowFtpingModule"/>, 用 <see cref="FluentFtpClient"/> 顶替契约包的 <see cref="IFtpClient"/>
/// <para>顶替靠 <see cref="FluentFtpClient"/> 上的 <c>[Dependency(ReplaceServices = true)]</c> 由约定注册完成, 这里不需要写替换代码。</para>
/// <para>这里额外注册代码页编码提供程序: 文件名编码填 gb2312 / gbk 这类非 Unicode 名字时, .NET 默认不认识。</para>
/// </summary>
[DependsOn(typeof(SnowFtpingModule))]
public sealed class SnowFtpingFluentFTPModule : SnowModule
{
    /// <inheritdoc />
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }
}

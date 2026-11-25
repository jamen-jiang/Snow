using Microsoft.Extensions.DependencyInjection;
using Snow.Modularity;

namespace Snow.VirtualFileSystem;

/// <summary>
/// 虚拟文件系统模块(对应 ABP 的 <c>AbpVirtualFileSystemModule</c>)
/// </summary>
public class SnowVirtualFileSystemModule : SnowModule
{
    /// <inheritdoc />
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.Services.AddSingleton<IVirtualFileProvider, SnowVirtualFileProvider>();
    }
}

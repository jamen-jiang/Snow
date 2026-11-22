using Microsoft.Extensions.DependencyInjection;
using Snow.Modularity;

namespace Snow.VirtualFileSystem;

/// <summary>
/// 虚拟文件系统模块(对应 ABP 的 <c>AbpVirtualFileSystemModule</c>)
/// <para>登记 <see cref="IVirtualFileProvider"/> 单例; 文件集由
/// <see cref="SnowVirtualFileSystemServiceCollectionExtensions.AddSnowVirtualFileSystem"/> 追加。</para>
/// </summary>
public sealed class SnowVirtualFileSystemModule : SnowModule
{
    /// <inheritdoc />
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.Services.AddSingleton<IVirtualFileProvider, SnowVirtualFileProvider>();
    }
}

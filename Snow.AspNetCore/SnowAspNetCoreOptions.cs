using Microsoft.Extensions.FileProviders;

namespace Snow.AspNetCore;

/// <summary>
/// Web 宿主选项, 绑定配置节 AspNetCore
/// </summary>
public sealed class SnowAspNetCoreOptions
{
    /// <summary>
    /// 是否把 <see cref="StaticAssetFileProviders"/> 里的文件提供器合并进 WebRoot, 默认 true
    /// </summary>
    public bool MergeStaticAssets { get; set; } = true;

    /// <summary>
    /// 额外静态资源提供器(包内 wwwroot、嵌入式资源等), 启动时合并进 WebRootFileProvider。
    /// 只能用代码登记(<c>AddSnowStaticAssets</c>), 不参与配置绑定。
    /// </summary>
    public IList<IFileProvider> StaticAssetFileProviders { get; } = new List<IFileProvider>();
}

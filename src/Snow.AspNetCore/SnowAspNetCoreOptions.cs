using Microsoft.Extensions.FileProviders;

namespace Snow.AspNetCore;

/// <summary>
/// Web 宿主选项, 绑定配置节 Snow(框架级开关统一放这里, 单个能力包的开关才用能力名)
/// </summary>
public sealed class SnowAspNetCoreOptions
{
    /// <summary>
    /// 是否把 <see cref="StaticAssetFileProviders"/> 里的文件提供器合并进 WebRoot, 默认 true
    /// <para>置 false 时已登记的提供器一个都不用, 启动日志会记一条警告列出被跳过的提供器。
    /// 配置键 <c>Snow:MergeStaticAssets</c>。</para>
    /// </summary>
    public bool MergeStaticAssets { get; set; } = true;

    /// <summary>
    /// 额外静态资源提供器(包内 wwwroot、嵌入式资源等), 启动时合并进 WebRootFileProvider。
    /// 只能用代码登记(<c>AddSnowStaticAssets</c>), 不参与配置绑定。
    /// </summary>
    public IList<IFileProvider> StaticAssetFileProviders { get; } = new List<IFileProvider>();
}

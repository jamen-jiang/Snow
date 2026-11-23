using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 没有真实 Web 环境时的占位实现(对应 ABP 的 <c>EmptyHostingEnvironment</c>)
/// <para>只用于让 <see cref="SnowAspNetCoreServiceCollectionExtensions.GetHostingEnvironment"/> 的调用方免于判空;
/// 六项属性都没有实际内容, 拿它去读文件或拼路径没有意义。</para>
/// </summary>
internal sealed class EmptyHostingEnvironment : IWebHostEnvironment
{
    /// <inheritdoc />
    public string EnvironmentName { get; set; } = default!;

    /// <inheritdoc />
    public string ApplicationName { get; set; } = default!;

    /// <inheritdoc />
    public string WebRootPath { get; set; } = default!;

    /// <inheritdoc />
    public IFileProvider WebRootFileProvider { get; set; } = default!;

    /// <inheritdoc />
    public string ContentRootPath { get; set; } = default!;

    /// <inheritdoc />
    public IFileProvider ContentRootFileProvider { get; set; } = default!;
}

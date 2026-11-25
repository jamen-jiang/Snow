using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace Snow.Localization;

/// <summary>
/// 字符串本地化器工厂: 按 <see cref="LocalizationResourceNameAttribute"/> 或类型全名决定资源名
/// </summary>
public sealed class SnowStringLocalizerFactory : IStringLocalizerFactory
{
    private readonly IOptions<SnowLocalizationOptions> _options;
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="options">本地化选项</param>
    /// <param name="serviceProvider">服务提供者, 用于加载资源的 json 文件</param>
    public SnowStringLocalizerFactory(IOptions<SnowLocalizationOptions> options, IServiceProvider serviceProvider)
    {
        _options = options;
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public IStringLocalizer Create(Type resourceSource)
    {
        ArgumentNullException.ThrowIfNull(resourceSource);

        string resourceName = LocalizationResourceNameAttribute.GetName(resourceSource);

        return new SnowStringLocalizer(resourceName, _options, _serviceProvider);
    }

    /// <inheritdoc />
    public IStringLocalizer Create(string baseName, string location)
        => new SnowStringLocalizer(string.IsNullOrWhiteSpace(baseName) ? location : baseName, _options, _serviceProvider);
}

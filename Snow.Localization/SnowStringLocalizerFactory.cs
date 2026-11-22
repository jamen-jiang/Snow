using System.Reflection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace Snow.Localization;

/// <summary>
/// 字符串本地化器工厂: 按 <see cref="LocalizationResourceAttribute"/> 或类型全名决定资源名
/// </summary>
public sealed class SnowStringLocalizerFactory : IStringLocalizerFactory
{
    private readonly IOptions<SnowLocalizationOptions> _options;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="options">本地化选项</param>
    public SnowStringLocalizerFactory(IOptions<SnowLocalizationOptions> options) => _options = options;

    /// <inheritdoc />
    public IStringLocalizer Create(Type resourceSource)
    {
        ArgumentNullException.ThrowIfNull(resourceSource);

        string resourceName = resourceSource.GetCustomAttribute<LocalizationResourceAttribute>(inherit: true)?.Name
            ?? resourceSource.FullName
            ?? resourceSource.Name;

        return new SnowStringLocalizer(resourceName, _options);
    }

    /// <inheritdoc />
    public IStringLocalizer Create(string baseName, string location)
        => new SnowStringLocalizer(string.IsNullOrWhiteSpace(baseName) ? location : baseName, _options);
}

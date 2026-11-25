using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace Snow.Localization;

/// <summary>
/// 本地化上下文(对应 ABP 的 <c>LocalizationContext</c>)
/// </summary>
public class LocalizationContext
{
    /// <summary>
    /// 服务提供器
    /// </summary>
    public IServiceProvider ServiceProvider { get; }

    /// <summary>
    /// 字符串本地化器工厂
    /// </summary>
    public IStringLocalizerFactory LocalizerFactory { get; }

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="serviceProvider">服务提供器</param>
    public LocalizationContext(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
        LocalizerFactory = ServiceProvider.GetRequiredService<IStringLocalizerFactory>();
    }
}

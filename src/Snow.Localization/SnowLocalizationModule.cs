using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Snow.Modularity;

namespace Snow.Localization;

/// <summary>
/// 本地化模块(对应 ABP 的 <c>AbpLocalizationModule</c>)
/// <para>登记 <see cref="IStringLocalizerFactory"/>; 资源由各模块通过
/// <c>Configure&lt;SnowLocalizationOptions&gt;</c> 追加。</para>
/// </summary>
public sealed class SnowLocalizationModule : SnowModule
{
    /// <inheritdoc />
    /// <param name="context">上下文</param>
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!context.Services.Any(descriptor => descriptor.ServiceType == typeof(SnowStringLocalizerFactory)))
        {
            context.Services.AddSingleton<SnowStringLocalizerFactory>();
            context.Services.AddSingleton<IStringLocalizerFactory>(serviceProvider =>
                serviceProvider.GetRequiredService<SnowStringLocalizerFactory>());
        }

        // 开放泛型: 让 IStringLocalizer<T> 能按 [LocalizationResourceName] 资源名解析(与 ABP 一致)
        if (!context.Services.Any(descriptor => descriptor.ServiceType == typeof(IStringLocalizer<>)))
        {
            context.Services.AddSingleton(typeof(IStringLocalizer<>), typeof(StringLocalizer<>));
        }
    }
}
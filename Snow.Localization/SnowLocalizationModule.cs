using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Snow.Modularity;

namespace Snow.Localization;

/// <summary>
/// 本地化模块(对应 ABP 的 <c>AbpLocalizationModule</c>)
/// <para>登记 <see cref="IStringLocalizerFactory"/>; 资源从配置节 <c>Localization</c> 或
/// <see cref="SnowLocalizationServiceCollectionExtensions.AddSnowLocalizationResource"/> 来。</para>
/// </summary>
public sealed class SnowLocalizationModule : SnowModule
{
    /// <inheritdoc />
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Configuration.GetSection("Localization").Exists())
        {
            Configure<SnowLocalizationOptions>(context.Configuration.GetSection("Localization"));
        }

        if (!context.Services.Any(descriptor => descriptor.ServiceType == typeof(SnowStringLocalizerFactory)))
        {
            context.Services.AddSingleton<SnowStringLocalizerFactory>();
            context.Services.AddSingleton<IStringLocalizerFactory>(serviceProvider =>
                serviceProvider.GetRequiredService<SnowStringLocalizerFactory>());
        }
    }
}

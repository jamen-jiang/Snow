using Snow.ExceptionHandling.Localization;
using Snow.Localization;
using Snow.Modularity;
using Snow.VirtualFileSystem;

namespace Snow.ExceptionHandling;

/// <summary>
/// 异常处理模块(对应 ABP 的 <c>AbpExceptionHandlingModule</c>)
/// <para>职责与 ABP 一致: 只登记内置本地化资源。转换器走 <c>[TransientService]</c> 约定注册,
/// 选项由框架统一的 <c>AddOptions()</c> 提供。</para>
/// </summary>
[DependsOn(typeof(SnowLocalizationModule), typeof(SnowVirtualFileSystemModule))]
public class SnowExceptionHandlingModule : SnowModule
{
    /// <inheritdoc />
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        Configure<SnowVirtualFileSystemOptions>(options =>
        {
            options.FileSets.AddEmbedded<SnowExceptionHandlingModule>();
        });

        Configure<SnowLocalizationOptions>(options =>
        {
            options.Resources
                .Add<SnowExceptionHandlingResource>("zh-Hans")
                .AddVirtualJson("/Snow/ExceptionHandling/Localization");
        });
    }
}

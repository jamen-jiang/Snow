using Snow.Modularity;

namespace Snow.Timing;

/// <summary>
/// 时间模块(对应 ABP 的 <c>AbpTimingModule</c>): 登记时钟选项
/// <para><see cref="Clock"/> / <see cref="CurrentTimezoneProvider"/> / <see cref="TZConvertTimezoneProvider"/>
/// 靠生命周期注解参与约定注册。</para>
/// </summary>
public sealed class SnowTimingModule : SnowModule
{
    /// <inheritdoc />
    /// <param name="context">上下文</param>
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        Configure<SnowClockOptions>(_ => { });
    }
}

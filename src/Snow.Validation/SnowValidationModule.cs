using Microsoft.Extensions.DependencyInjection;
using Snow.Castle;
using Snow.Modularity;

namespace Snow.Validation;

/// <summary>
/// 校验模块(对应 ABP 的 <c>AbpValidationModule</c>)
/// <para>登记对象校验器; 依赖 AOP 才能让 [Validation] 生效。</para>
/// <para>登记为具体类型再转发到契约, 这样集成包可以取到它做合成。</para>
/// <para>选项不读配置节(与 ABP 一致): 宿主在 <c>PreConfigureServices</c> 里
/// <c>Configure&lt;SnowValidationOptions&gt;(context.Configuration.GetSection("Validation"))</c> 或直接改属性。</para>
/// </summary>
[DependsOn(typeof(SnowCastleCoreModule))]
public sealed class SnowValidationModule : SnowModule
{
    /// <inheritdoc />
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // 对齐 ABP: 校验拦截器由登记器按 [Validation] 挂上去
        context.Services.OnRegistered(SnowValidationInterceptorRegistrar.RegisterIfNeeded);
        context.Services.AddSingleton<DataAnnotationObjectValidator>();
        context.Services.AddSingleton<ISnowObjectValidator>(serviceProvider =>
            serviceProvider.GetRequiredService<DataAnnotationObjectValidator>());
    }
}

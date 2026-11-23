using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Snow.Aop.Castle;
using Snow.DependencyInjection;
using Snow.Modularity;

namespace Snow.Validation;

/// <summary>
/// 校验模块(对应 ABP 的 <c>AbpValidationModule</c>)
/// <para>登记对象校验器并从配置节 <c>Validation</c> 读选项; 依赖 AOP 才能让 [SnowValidation] 生效。</para>
/// <para>登记为具体类型再转发到契约, 这样集成包可以取到它做合成。</para>
/// </summary>
[DependsOn(typeof(SnowAopModule))]
public sealed class SnowValidationModule : SnowModule
{
    /// <inheritdoc />
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Configuration.GetSection("Validation").Exists())
        {
            Configure<SnowValidationOptions>(context.Configuration.GetSection("Validation"));
        }

        context.Services.AddSingleton<DataAnnotationObjectValidator>();
        context.Services.AddSingleton<ISnowObjectValidator>(serviceProvider =>
            serviceProvider.GetRequiredService<DataAnnotationObjectValidator>());
    }
}

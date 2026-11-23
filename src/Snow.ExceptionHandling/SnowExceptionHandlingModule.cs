using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Snow.Modularity;
using Snow.Validation;

namespace Snow.ExceptionHandling;

/// <summary>
/// 异常处理模块(对应 ABP 的 <c>AbpExceptionHandlingModule</c>)
/// <para>登记异常转换器并从配置节 <c>ExceptionHandling</c> 读选项; 依赖校验包以便翻译校验异常。</para>
/// </summary>
[DependsOn(typeof(SnowValidationModule))]
public sealed class SnowExceptionHandlingModule : SnowModule
{
    /// <inheritdoc />
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Configuration.GetSection("ExceptionHandling").Exists())
        {
            Configure<SnowExceptionHandlingOptions>(context.Configuration.GetSection("ExceptionHandling"));
        }

        context.Services.AddSingleton<IExceptionToErrorInfoConverter, DefaultExceptionToErrorInfoConverter>();
    }
}

using Microsoft.Extensions.DependencyInjection;

namespace Snow.ExceptionHandling;

/// <summary>
/// 异常处理的服务登记入口
/// </summary>
public static class SnowExceptionHandlingServiceCollectionExtensions
{
    /// <summary>
    /// 调整异常处理选项
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configure">选项配置动作</param>
    /// <returns>服务集合本身, 便于链式调用</returns>
    public static IServiceCollection AddSnowExceptionHandling(this IServiceCollection services, Action<SnowExceptionHandlingOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.Configure(configure);
        return services;
    }

    /// <summary>
    /// 按容器里登记的转换器把异常翻译成错误响应
    /// </summary>
    /// <param name="services">服务集合, 通常传 app.Services</param>
    /// <param name="exception">待翻译的异常</param>
    /// <returns>错误响应信封</returns>
    public static SnowRemoteServiceErrorResponse ToErrorResponse(this IServiceProvider services, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(exception);

        IExceptionToErrorInfoConverter converter = services.GetService<IExceptionToErrorInfoConverter>()
            ?? new DefaultExceptionToErrorInfoConverter(
                Microsoft.Extensions.Options.Options.Create(new SnowExceptionHandlingOptions()));

        return new SnowRemoteServiceErrorResponse(converter.Convert(exception));
    }
}

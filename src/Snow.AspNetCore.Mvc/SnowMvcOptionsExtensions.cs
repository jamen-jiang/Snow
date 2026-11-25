using System.Linq;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Snow.AspNetCore.Mvc.ContentFormatters;
using Snow.Json.SystemTextJson.JsonConverters;
using Microsoft.Extensions.DependencyInjection;
using Snow.AspNetCore.Mvc.Conventions;
using Snow.AspNetCore.Mvc.ExceptionHandling;
using Snow.AspNetCore.Mvc.Response;
using Snow.AspNetCore.Mvc.Validation;

namespace Snow.AspNetCore.Mvc;

/// <summary>
/// MVC 选项扩展。
/// <para>把约定路由、全局过滤器等统一挂到这里, 模块只负责调用一次
/// <see cref="AddSnow(MvcOptions, IServiceProvider)"/>。</para>
/// </summary>
internal static class SnowMvcOptionsExtensions
{
    /// <summary>
    /// 应用 Snow 的 MVC 选项配置。
    /// </summary>
    /// <param name="options">MVC 选项</param>
    /// <param name="serviceProvider">服务提供者, 约定路由从容器创建</param>
    public static void AddSnow(this MvcOptions options, IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(serviceProvider);

        AddConventions(options, serviceProvider);
        AddActionFilters(options);
        AddFormatters(options);
    }

    private static void AddFormatters(MvcOptions options)
    {
        options.OutputFormatters.Insert(0, new RemoteStreamContentOutputFormatter());
        var systemTextJsonOutputFormatter = options.OutputFormatters
            .OfType<SystemTextJsonOutputFormatter>()
            .FirstOrDefault();

        if (systemTextJsonOutputFormatter != null)
        {
            var index = options.OutputFormatters.IndexOf(systemTextJsonOutputFormatter);
            var jsonOptions = new JsonSerializerOptions(systemTextJsonOutputFormatter.SerializerOptions);
            jsonOptions.Converters.RemoveAll(x => x is ObjectToInferredTypesConverter);
            options.OutputFormatters[index] = new SystemTextJsonOutputFormatter(jsonOptions);
        }
    }

    private static void AddConventions(MvcOptions options, IServiceProvider serviceProvider)
    {
        options.Conventions.Add(serviceProvider.GetRequiredService<SnowServiceConvention>());
    }

    private static void AddActionFilters(MvcOptions options)
    {
        options.Filters.AddService(typeof(SnowNoContentActionFilter));
        options.Filters.AddService(typeof(SnowValidationActionFilter));
        options.Filters.AddService(typeof(SnowExceptionFilter));
    }

}

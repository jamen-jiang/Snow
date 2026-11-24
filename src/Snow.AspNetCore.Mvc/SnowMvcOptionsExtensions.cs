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
/// MVC 选项扩展, 对应 ABP 的 <c>AbpMvcOptionsExtensions</c>。
/// <para>把约定路由、全局过滤器等统一挂到这里, 模块只负责调用一次
/// <see cref="AddSnow(MvcOptions, IServiceCollection)"/>。</para>
/// </summary>
internal static class SnowMvcOptionsExtensions
{
    /// <summary>
    /// 应用 Snow 的 MVC 选项配置。
    /// </summary>
    /// <param name="options">MVC 选项</param>
    /// <param name="services">服务集合, 约定路由需要创建 Wrapper</param>
    public static void AddSnow(this MvcOptions options, IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(services);

        AddConventions(options, services);
        AddActionFilters(options);
        AddPageFilters(options);
        AddFormatters(options);
    }

    private static void AddFormatters(MvcOptions options)
    {
        options.OutputFormatters.Insert(0, new RemoteStreamContentOutputFormatter());
        var systemTextJsonOutputFormatter = options.OutputFormatters
            .Where(f => f is SystemTextJsonOutputFormatter)
            .Cast<SystemTextJsonOutputFormatter>().FirstOrDefault();

        if (systemTextJsonOutputFormatter != null)
        {
            options.OutputFormatters.Remove(systemTextJsonOutputFormatter);
            var jsonOptions = new JsonSerializerOptions(systemTextJsonOutputFormatter.SerializerOptions);
            jsonOptions.Converters.RemoveAll(x => x is ObjectToInferredTypesConverter);
            options.OutputFormatters.Add(new SystemTextJsonOutputFormatter(jsonOptions));
        }
    }

    private static void AddConventions(MvcOptions options, IServiceCollection services)
    {
        options.Conventions.Add(new SnowServiceConventionWrapper(services));
    }

    private static void AddActionFilters(MvcOptions options)
    {
        options.Filters.AddService(typeof(SnowNoContentActionFilter));
        options.Filters.AddService(typeof(SnowValidationActionFilter));
        options.Filters.AddService(typeof(SnowExceptionFilter));
    }

    private static void AddPageFilters(MvcOptions options)
    {
        // 预留 View/PageFilter 的注册入口; 当前纯 API 不需要页面过滤器。
    }
}

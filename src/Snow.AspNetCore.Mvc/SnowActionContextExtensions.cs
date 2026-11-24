using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Snow.AspNetCore.Mvc;

/// <summary>
/// 从 <see cref="FilterContext"/> 取服务的扩展(对应 ABP 的 <c>AbpActionContextExtensions</c>)
/// </summary>
public static class SnowActionContextExtensions
{
    public static T GetRequiredService<T>(this FilterContext context)
        where T : class
    {
        return context.HttpContext.RequestServices.GetRequiredService<T>();
    }

    public static T? GetService<T>(this FilterContext context, T? defaultValue = default)
        where T : class
    {
        return context.HttpContext.RequestServices.GetService<T>() ?? defaultValue;
    }
}

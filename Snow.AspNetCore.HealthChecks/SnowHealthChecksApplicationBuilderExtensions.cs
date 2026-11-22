using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Snow.AspNetCore.HealthChecks;

/// <summary>
/// 健康检查端点挂载
/// </summary>
public static class SnowHealthChecksApplicationBuilderExtensions
{
    /// <summary>
    /// 挂上健康检查端点, 响应固定为 JSON
    /// </summary>
    /// <param name="endpoints">端点路由构建器</param>
    /// <returns>端点约定构建器</returns>
    public static IEndpointConventionBuilder MapSnowHealthChecks(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        SnowHealthChecksOptions options = endpoints.ServiceProvider
            .GetRequiredService<IOptions<SnowHealthChecksOptions>>()
            .Value;

        IEndpointConventionBuilder builder = endpoints.MapHealthChecks(options.Path, new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            ResponseWriter = (context, report) => WriteResponseAsync(context, report, options.IncludeDetails),
        });

        if (options.RequireAuthorization)
        {
            builder.RequireAuthorization();
        }
        else
        {
            builder.AllowAnonymous();
        }

        return builder;
    }

    private static Task WriteResponseAsync(HttpContext context, HealthReport report, bool includeDetails)
    {
        context.Response.ContentType = "application/json; charset=utf-8";

        object payload = includeDetails
            ? new
            {
                status = report.Status.ToString(),
                totalDuration = report.TotalDuration.TotalMilliseconds,
                checks = report.Entries.Select(entry => new
                {
                    name = entry.Key,
                    status = entry.Value.Status.ToString(),
                    duration = entry.Value.Duration.TotalMilliseconds,
                    description = entry.Value.Description,
                    exception = entry.Value.Exception?.Message,
                }),
            }
            : new
            {
                status = report.Status.ToString(),
                totalDuration = report.TotalDuration.TotalMilliseconds,
            };

        return context.Response.WriteAsJsonAsync(payload);
    }
}

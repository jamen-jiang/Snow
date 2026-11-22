using Elsa.Extensions;
using Microsoft.AspNetCore.Builder;

namespace Snow.Workflows.Elsa;

/// <summary>
/// 宿主启动管道的接入点。
/// </summary>
public static class SnowElsaApplicationBuilderExtensions
{
    /// <summary>
    /// 挂载 Elsa 的 HTTP 端点。放在 UseRouting 之后。
    /// </summary>
    public static IApplicationBuilder UseSnowWorkflows(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.UseWorkflows();
    }
}

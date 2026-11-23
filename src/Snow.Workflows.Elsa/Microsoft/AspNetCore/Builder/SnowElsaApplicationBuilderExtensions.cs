using Elsa.Extensions;
using Microsoft.AspNetCore.Builder;
using Snow.Workflows.Elsa;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// 工作流 HTTP 中间件: 把 Elsa 的 <c>HttpWorkflowsMiddleware</c> 挂到管道上
/// <para>由 <see cref="SnowElsaModule"/> 在应用初始化收尾阶段自动调用, 落点是授权之后、端点之前 ——
/// 它是终端分支(命中工作流路径就自己写响应, 不调 next), 早于授权挂就对匿名请求可见。</para>
/// </summary>
public static class SnowElsaApplicationBuilderExtensions
{
    /// <summary>
    /// 挂上 Elsa 的工作流 HTTP 中间件
    /// </summary>
    /// <param name="app">应用构建器</param>
    /// <returns>同一个应用构建器, 便于链式调用</returns>
    public static IApplicationBuilder UseSnowWorkflows(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.UseWorkflows();
    }
}

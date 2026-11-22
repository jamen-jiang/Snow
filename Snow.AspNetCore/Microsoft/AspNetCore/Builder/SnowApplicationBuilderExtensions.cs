using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Snow.AspNetCore;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// 管道组装入口
/// </summary>
public static class SnowApplicationBuilderExtensions
{
    private const string Marker = "Snow.ApplicationBuilder";

    /// <summary>
    /// 拿到管道组装器, 由它展开框架的默认骨架并执行各包登记的过滤器
    /// <para>常规宿主不用调: 框架的启动过滤器在全部模块初始化完成之后会自己展开一次。
    /// 只有要接管管道(比如换掉整套骨架)时才在启动模块里自己调, 先调的那次生效, 后调的直接拿到同一个实例。</para>
    /// <para>示例: <c>app.UseSnow().WithMiddleware(_ => { }).WithEndpoints(_ => { });</c></para>
    /// </summary>
    /// <param name="app">应用构建器</param>
    /// <returns>管道组装器</returns>
    public static ISnowApplicationBuilder UseSnow(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        if (app.Properties.TryGetValue(Marker, out object? existing) && existing is ISnowApplicationBuilder builder)
        {
            return builder;
        }

        SnowApplicationBuilder created = new(app);
        app.Properties[Marker] = created;
        return created;
    }
}

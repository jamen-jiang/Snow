using Microsoft.AspNetCore.Builder;
using Snow.DependencyInjection;
using Snow.Modularity;

namespace Snow.AspNetCore;

/// <summary>
/// 应用初始化上下文的宿主对象扩展(对应 ABP 的 <c>ApplicationInitializationContextExtensions</c>)
/// <para>只提供模块初始化阶段真正需要的那一个对象: 应用构建器。宿主、端点路由构建器、Web 应用、环境、配置、
/// 日志工厂都能从 <c>context.ServiceProvider</c> 直接取, 不必在这里再各包一层。</para>
/// <para>依赖 ASP.NET Core 类型, 所以放在 Snow.AspNetCore 而不是 Snow.Core —— 与 ABP 把它放在
/// Volo.Abp.AspNetCore 同理; 控制台/Worker 宿主不必被迫拉入 Web 框架。</para>
/// <para>两个版本: 带 <c>OrNull</c> 的取不到返回 null, 不带的取不到抛
/// <see cref="SnowInitializationException"/>, 便于模块在非 Web 宿主里先用 <c>OrNull</c> 版本判断。</para>
/// </summary>
public static class ApplicationInitializationContextExtensions
{
    /// <summary>
    /// 取应用构建器: 模块在 <c>OnApplicationInitialization</c> 里挂中间件时用它
    /// </summary>
    /// <param name="context">应用初始化上下文</param>
    /// <returns>应用构建器</returns>
    /// <exception cref="SnowInitializationException">宿主还没把应用构建器放进对象访问器</exception>
    public static IApplicationBuilder GetApplicationBuilder(this ApplicationInitializationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.GetApplicationBuilderOrNull() ?? throw new SnowInitializationException(
            "应用构建器尚未就绪: 宿主还没有调用 app.InitializeApplicationAsync(), 或启动模块没有依赖 SnowAspNetCoreModule。");
    }

    /// <summary>
    /// 取应用构建器, 取不到返回 null(非 Web 宿主)
    /// </summary>
    /// <param name="context">应用初始化上下文</param>
    /// <returns>应用构建器或 null</returns>
    public static IApplicationBuilder? GetApplicationBuilderOrNull(this ApplicationInitializationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.ServiceProvider.GetObjectOrNull<IApplicationBuilder>();
    }
}

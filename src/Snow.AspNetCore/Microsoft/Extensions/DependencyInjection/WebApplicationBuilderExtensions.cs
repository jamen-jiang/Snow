using Microsoft.AspNetCore.Builder;
using Snow;
using Snow.DependencyInjection;
using Snow.Modularity;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Web 宿主的应用装配入口(对应 ABP 的 <c>WebApplicationBuilderExtensions</c>, 同在
/// <c>Microsoft.Extensions.DependencyInjection</c> 命名空间, 宿主不必额外 using)
/// <para>比直接调 <c>builder.Services.AddApplicationAsync(...)</c> 多的就两件事, 与 ABP 逐行对应:
/// 把 <c>builder.Configuration</c> 顶进容器(宿主不用再手写 <c>AddSingleton&lt;IConfiguration&gt;</c>),
/// 以及把 <c>builder.Environment.EnvironmentName</c> 抄进应用创建选项(宿主不用再自己填环境名)。</para>
/// </summary>
public static class WebApplicationBuilderExtensions
{
    /// <summary>
    /// 装配启动模块及其全部依赖, 并立即执行服务注册阶段
    /// </summary>
    /// <typeparam name="TStartupModule">启动模块类型</typeparam>
    /// <param name="builder">Web 应用构建器</param>
    /// <param name="optionsAction">应用创建选项; 里面填的 Environment 优先于 builder 的环境名</param>
    /// <returns>创建好的应用实例</returns>
    public static async Task<SnowApplication> AddApplicationAsync<TStartupModule>(
        this WebApplicationBuilder builder,
        Action<SnowApplicationCreationOptions>? optionsAction = null)
        where TStartupModule : ISnowModule
    {
        return await builder.Services.AddApplicationAsync<TStartupModule>(options =>
        {
            options.Services.ReplaceConfiguration(builder.Configuration);
            optionsAction?.Invoke(options);
            if (options.Environment.IsNullOrWhiteSpace())
            {
                options.Environment = builder.Environment.EnvironmentName;
            }
        });
    }

    /// <summary>
    /// 按类型装配启动模块, 其余同 <see cref="AddApplicationAsync{TStartupModule}"/>
    /// </summary>
    /// <param name="builder">Web 应用构建器</param>
    /// <param name="startupModuleType">启动模块类型</param>
    /// <param name="optionsAction">应用创建选项; 里面填的 Environment 优先于 builder 的环境名</param>
    /// <returns>创建好的应用实例</returns>
    public static async Task<SnowApplication> AddApplicationAsync(
        this WebApplicationBuilder builder,
        Type startupModuleType,
        Action<SnowApplicationCreationOptions>? optionsAction = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return await builder.Services.AddApplicationAsync(startupModuleType, options =>
        {
            options.Services.ReplaceConfiguration(builder.Configuration);
            optionsAction?.Invoke(options);
            if (options.Environment.IsNullOrWhiteSpace())
            {
                options.Environment = builder.Environment.EnvironmentName;
            }
        });
    }
}

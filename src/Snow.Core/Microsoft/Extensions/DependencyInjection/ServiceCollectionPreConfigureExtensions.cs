using System;
using Snow.DependencyInjection;
using Snow.Options;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// <see cref="IServiceCollection"/> 的预配置扩展(对应 ABP 的 <c>ServiceCollectionPreConfigureExtensions</c>):
/// 在容器构建前收集一批预配置委托, 待选项实例可用时统一应用。
/// </summary>
public static class ServiceCollectionPreConfigureExtensions
{
    /// <summary>
    /// 追加一个预配置动作。
    /// </summary>
    public static IServiceCollection PreConfigure<TOptions>(this IServiceCollection services, Action<TOptions> optionsAction)
    {
        services.GetPreConfigureActions<TOptions>().Add(optionsAction);
        return services;
    }

    /// <summary>
    /// 对一个新建的选项实例执行全部预配置动作。
    /// </summary>
    public static TOptions ExecutePreConfiguredActions<TOptions>(this IServiceCollection services)
        where TOptions : new()
    {
        return services.ExecutePreConfiguredActions(new TOptions());
    }

    /// <summary>
    /// 对给定选项实例执行全部预配置动作。
    /// </summary>
    public static TOptions ExecutePreConfiguredActions<TOptions>(this IServiceCollection services, TOptions options)
    {
        services.GetPreConfigureActions<TOptions>().Configure(options);
        return options;
    }

    /// <summary>
    /// 取(必要时创建)某选项类型的预配置动作集合。
    /// </summary>
    public static PreConfigureActionList<TOptions> GetPreConfigureActions<TOptions>(this IServiceCollection services)
    {
        var actionList = services.GetSingletonInstanceOrNull<IObjectAccessor<PreConfigureActionList<TOptions>>>()?.Value;
        if (actionList == null)
        {
            actionList = new PreConfigureActionList<TOptions>();
            services.AddObjectAccessor(actionList);
        }

        return actionList;
    }
}

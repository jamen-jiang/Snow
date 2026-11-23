using System.Reflection;

namespace Snow;

/// <summary>
/// 读取控制器上的 <see cref="DisableSnowFeaturesAttribute"/>: 框架各处按类型判断该不该跳过
/// <para>类型为 null(不是控制器端点)时一律返回 <see langword="false"/>, 即不跳过。</para>
/// </summary>
public static class SnowFeatureSwitches
{
    /// <summary>
    /// 该类型是否关闭了拦截器
    /// </summary>
    /// <param name="type">控制器类型; 可为 null</param>
    /// <returns>关闭了返回 <see langword="true"/></returns>
    public static bool IsInterceptorsDisabled(Type? type) => GetAttribute(type)?.DisableInterceptors ?? false;

    /// <summary>
    /// 该类型是否关闭了框架中间件
    /// </summary>
    /// <param name="type">控制器类型; 可为 null</param>
    /// <returns>关闭了返回 <see langword="true"/></returns>
    public static bool IsMiddlewareDisabled(Type? type) => GetAttribute(type)?.DisableMiddleware ?? false;

    /// <summary>
    /// 该类型是否关闭了 MVC 过滤器
    /// </summary>
    /// <param name="type">控制器类型; 可为 null</param>
    /// <returns>关闭了返回 <see langword="true"/></returns>
    public static bool IsMvcFiltersDisabled(Type? type) => GetAttribute(type)?.DisableMvcFilters ?? false;

    private static DisableSnowFeaturesAttribute? GetAttribute(Type? type)
    {
        return type?.GetCustomAttribute<DisableSnowFeaturesAttribute>(inherit: true);
    }
}

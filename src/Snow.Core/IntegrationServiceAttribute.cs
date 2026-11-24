using System;

namespace Snow;

/// <summary>
/// 标记集成服务(对应 ABP 的 <c>IntegrationServiceAttribute</c>), 用来区分应用服务与对外集成服务。
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
public class IntegrationServiceAttribute : Attribute
{
    /// <summary>
    /// 指定类型是否直接或通过接口继承了本特性
    /// </summary>
    public static bool IsDefinedOrInherited<T>() => IsDefinedOrInherited(typeof(T));

    /// <summary>
    /// 指定类型是否直接或通过接口继承了本特性
    /// </summary>
    /// <param name="type">类型, 可为 null</param>
    public static bool IsDefinedOrInherited(Type type)
    {
        if (type.IsDefined(typeof(IntegrationServiceAttribute), true))
        {
            return true;
        }

        foreach (var @interface in type.GetInterfaces())
        {
            if (@interface.IsDefined(typeof(IntegrationServiceAttribute), true))
            {
                return true;
            }
        }

        return false;
    }
}
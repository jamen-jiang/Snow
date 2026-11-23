using System.Collections.Concurrent;
using System.Reflection;

namespace Snow.DependencyInjection;

/// <summary>
/// 程序集类型缓存:同一程序集只枚举一次 <see cref="Assembly.GetTypes"/>,
/// 多个模块同处一个程序集时不再重复反射,也避免部分类型加载失败时反复退化。
/// </summary>
public static class AssemblyTypeCache
{
    private static readonly ConcurrentDictionary<Assembly, Type[]> Cache = new();

    /// <summary>
    /// 取程序集可加载的全部类型,结果按程序集缓存
    /// </summary>
    /// <param name="assembly">程序集</param>
    /// <returns>类型数组</returns>
    public static Type[] GetTypes(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        return Cache.GetOrAdd(assembly, static asm => LoadTypes(asm));
    }

    private static Type[] LoadTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            return exception.Types.Where(type => type is not null).Select(type => type!).ToArray();
        }
    }
}

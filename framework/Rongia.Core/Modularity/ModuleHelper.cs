using System.Reflection;
using Microsoft.Extensions.Logging;

namespace Rongia.Modularity;

/// <summary>
/// 模块辅助方法(对应 ABP 的 AbpModuleHelper)
/// </summary>
public static class ModuleHelper
{
    /// <summary>
    /// 从启动模块出发递归收集全部模块类型,被依赖的模块排在前面
    /// </summary>
    /// <param name="startupModuleType">启动模块类型</param>
    /// <param name="logger">可选的日志记录器</param>
    /// <returns>模块类型列表</returns>
    public static List<Type> FindAllModuleTypes(Type startupModuleType, ILogger? logger)
    {
        List<Type> moduleTypes = new List<Type>();

        logger?.Log(LogLevel.Debug, "已加载的模块:");

        AddModuleAndDependenciesRecursively(moduleTypes, new HashSet<Type>(), startupModuleType, logger);

        return moduleTypes;
    }

    /// <summary>
    /// 取模块通过 DependsOnAttribute 直接声明的依赖模块类型
    /// </summary>
    /// <param name="moduleType">模块类型</param>
    /// <returns>被依赖的模块类型</returns>
    public static List<Type> FindDependedModuleTypes(Type moduleType)
    {
        CheckModuleType(moduleType);

        List<Type> dependencies = new List<Type>();
        HashSet<Type> seen = new HashSet<Type>();

        foreach (object attribute in moduleType.GetCustomAttributes(true))
        {
            if (attribute is not IDependedTypesProvider provider)
            {
                continue;
            }

            foreach (Type dependedModuleType in provider.GetDependedTypes())
            {
                if (dependedModuleType != null && seen.Add(dependedModuleType))
                {
                    dependencies.Add(dependedModuleType);
                }
            }
        }

        return dependencies;
    }

    /// <summary>
    /// 校验类型是可实例化的模块类型
    /// </summary>
    /// <param name="moduleType">模块类型</param>
    /// <exception cref="ModuleLoadException">类型未继承 <see cref="Module"/> 或为抽象类型</exception>
    public static void CheckModuleType(Type moduleType)
    {
        ArgumentNullException.ThrowIfNull(moduleType);

        if (!typeof(Module).IsAssignableFrom(moduleType))
        {
            throw new ModuleLoadException("类型 " + moduleType.FullName + " 未继承 " + nameof(Rongia.Modularity.Module));
        }

        if (moduleType.IsAbstract)
        {
            throw new ModuleLoadException("类型 " + moduleType.FullName + " 是抽象类型,无法实例化");
        }
    }

    /// <summary>
    /// 实例化模块
    /// </summary>
    /// <param name="moduleType">模块类型</param>
    /// <returns>模块实例</returns>
    /// <exception cref="ModuleLoadException">类型非法或缺少公共无参构造器</exception>
    public static Module CreateInstance(Type moduleType)
    {
        CheckModuleType(moduleType);

        try
        {
            return (Module)Activator.CreateInstance(moduleType)!;
        }
        catch (Exception exception)
        {
            throw new ModuleLoadException("模块 " + moduleType.FullName + " 实例化失败,需要提供公共无参构造器", exception);
        }
    }

    private static void AddModuleAndDependenciesRecursively(List<Type> moduleTypes, HashSet<Type> visited, Type moduleType, ILogger? logger, int depth = 0)
    {
        CheckModuleType(moduleType);

        if (!visited.Add(moduleType))
        {
            return;
        }

        moduleTypes.Add(moduleType);

        logger?.Log(LogLevel.Debug, new string(' ', depth * 2) + "- " + moduleType.FullName);

        foreach (Type dependedModuleType in FindDependedModuleTypes(moduleType))
        {
            AddModuleAndDependenciesRecursively(moduleTypes, visited, dependedModuleType, logger, depth + 1);
        }
    }
}

using Microsoft.Extensions.DependencyInjection;

namespace Snow.Modularity;

/// <summary>
/// 模块加载器(对应 ABP 的 ModuleLoader):
/// 从启动模块出发收集全部模块类型,建立依赖图后按"被依赖者在前"排序。
/// </summary>
public static class ModuleLoader
{
    /// <summary>
    /// 加载启动模块及其全部依赖,返回按依赖顺序排好的模块
    /// </summary>
    /// <param name="services">服务集合,模块实例会以自身类型登记为单例</param>
    /// <param name="startupModuleType">启动模块类型</param>
    /// <returns>按依赖顺序排列的模块描述信息,被依赖者在前,启动模块排在最后</returns>
    public static IReadOnlyList<SnowModuleDescriptor> LoadModules(IServiceCollection services, Type startupModuleType)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(startupModuleType);

        List<SnowModuleDescriptor> modules = [];

        foreach (Type moduleType in SnowModuleHelper.FindAllModuleTypes(startupModuleType))
        {
            SnowModule module = SnowModuleHelper.CreateInstance(moduleType);

            services.AddSingleton(moduleType, module);

            modules.Add(new SnowModuleDescriptor(moduleType, module));
        }

        Dictionary<Type, SnowModuleDescriptor> modulesByType = new(modules.Count);

        foreach (SnowModuleDescriptor module in modules)
        {
            if (!modulesByType.TryAdd(module.Type, module))
            {
                throw new ModuleLoadException("同一个模块类型被加载了两次: " + module.Type.AssemblyQualifiedName);
            }
        }

        foreach (SnowModuleDescriptor module in modules)
        {
            foreach (Type dependedModuleType in SnowModuleHelper.FindDependedModuleTypes(module.Type))
            {
                if (!modulesByType.TryGetValue(dependedModuleType, out SnowModuleDescriptor? dependedModule))
                {
                    throw new ModuleLoadException(
                        "找不到模块 " + module.Type.AssemblyQualifiedName + " 依赖的模块 " + dependedModuleType.AssemblyQualifiedName);
                }

                module.AddDependency(dependedModule);
            }
        }

        List<SnowModuleDescriptor> sortedModules = new(modules.Count);
        Dictionary<Type, int> states = new(modules.Count);

        foreach (SnowModuleDescriptor module in modules)
        {
            Visit(module, sortedModules, states);
        }

        if (!modulesByType.TryGetValue(startupModuleType, out SnowModuleDescriptor? startupModule))
        {
            throw new ModuleLoadException("启动模块不在已加载的模块列表里: " + startupModuleType.AssemblyQualifiedName);
        }

        sortedModules.Remove(startupModule);
        sortedModules.Add(startupModule);

        return sortedModules;
    }

    private static void Visit(SnowModuleDescriptor module, List<SnowModuleDescriptor> sortedModules, Dictionary<Type, int> states)
    {
        // 1 = 正在访问(出现在当前路径上), 2 = 已完成
        if (states.TryGetValue(module.Type, out int state))
        {
            if (state == 1)
            {
                throw new ModuleLoadException("模块依赖出现循环: " + module.Type.AssemblyQualifiedName);
            }

            return;
        }

        states[module.Type] = 1;

        foreach (SnowModuleDescriptor dependency in module.Dependencies)
        {
            Visit(dependency, sortedModules, states);
        }

        states[module.Type] = 2;
        sortedModules.Add(module);
    }
}

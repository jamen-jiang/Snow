using Microsoft.Extensions.DependencyInjection;

namespace Snow.Modularity;

/// <summary>
/// 模块加载器(对应 ABP 的 ModuleLoader):
/// 从启动模块出发收集全部模块类型,建立依赖图后按"被依赖者在前"排序。
/// <para>类型索引只建一次:依赖解析、启动模块定位与容器查询共用同一个字典。</para>
/// </summary>
public class ModuleLoader
{
    /// <summary>
    /// 加载启动模块及其全部依赖,返回按依赖顺序排好的模块
    /// </summary>
    /// <param name="services">服务集合,模块实例会以自身类型登记为单例</param>
    /// <param name="startupModuleType">启动模块类型</param>
    /// <returns>模块加载结果</returns>
    public ModuleLoadResult LoadModules(IServiceCollection services, Type startupModuleType)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(startupModuleType);

        List<SnowModuleDescriptor> modules = new List<SnowModuleDescriptor>();

        FillModules(modules, services, startupModuleType);

        Dictionary<Type, SnowModuleDescriptor> modulesByType = IndexModules(modules);

        foreach (SnowModuleDescriptor module in modules)
        {
            SetDependencies(modulesByType, module);
        }

        IReadOnlyList<SnowModuleDescriptor> sortedModules = SortByDependency(modules, modulesByType, startupModuleType);

        return new ModuleLoadResult(sortedModules, modulesByType);
    }

    /// <summary>
    /// 从启动模块出发收集全部模块类型,逐个创建描述信息
    /// </summary>
    /// <param name="modules">收集结果</param>
    /// <param name="services">服务集合</param>
    /// <param name="startupModuleType">启动模块类型</param>
    protected virtual void FillModules(List<SnowModuleDescriptor> modules, IServiceCollection services, Type startupModuleType)
    {
        foreach (Type moduleType in SnowModuleHelper.FindAllModuleTypes(startupModuleType, null))
        {
            modules.Add(CreateModuleDescriptor(services, moduleType));
        }
    }

    /// <summary>
    /// 建立"模块类型 → 模块描述信息"的索引。这是整条链路上唯一一次建表,结果会随加载结果一起交给模块容器。
    /// </summary>
    /// <param name="modules">全部模块描述信息</param>
    /// <returns>模块类型索引</returns>
    /// <exception cref="ModuleLoadException">同一模块类型被加载了两次</exception>
    protected virtual Dictionary<Type, SnowModuleDescriptor> IndexModules(List<SnowModuleDescriptor> modules)
    {
        Dictionary<Type, SnowModuleDescriptor> modulesByType = new Dictionary<Type, SnowModuleDescriptor>(modules.Count);

        foreach (SnowModuleDescriptor module in modules)
        {
            if (!modulesByType.TryAdd(module.Type, module))
            {
                throw new ModuleLoadException("同一个模块类型被加载了两次: " + module.Type.AssemblyQualifiedName);
            }
        }

        return modulesByType;
    }

    /// <summary>
    /// 为单个模块解析出直接依赖的描述信息,全部走字典查找
    /// </summary>
    /// <param name="modulesByType">模块类型到描述信息的索引</param>
    /// <param name="module">待处理的模块</param>
    /// <exception cref="ModuleLoadException">依赖的模块不在已加载的模块列表里</exception>
    protected virtual void SetDependencies(IReadOnlyDictionary<Type, SnowModuleDescriptor> modulesByType, SnowModuleDescriptor module)
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

    /// <summary>
    /// 创建模块描述信息,同时把模块实例注册到服务集合
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="moduleType">模块类型</param>
    /// <returns>模块描述信息</returns>
    protected virtual SnowModuleDescriptor CreateModuleDescriptor(IServiceCollection services, Type moduleType)
    {
        return new SnowModuleDescriptor(moduleType, CreateAndRegisterModule(services, moduleType));
    }

    /// <summary>
    /// 实例化模块并以自身类型注册为单例
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="moduleType">模块类型</param>
    /// <returns>模块实例</returns>
    protected virtual SnowModule CreateAndRegisterModule(IServiceCollection services, Type moduleType)
    {
        SnowModule module = SnowModuleHelper.CreateInstance(moduleType);

        services.AddSingleton(moduleType, module);

        return module;
    }

    /// <summary>
    /// 按依赖关系拓扑排序,被依赖的模块排在前,启动模块固定排最后
    /// </summary>
    /// <param name="modules">全部模块描述信息</param>
    /// <param name="modulesByType">模块类型到描述信息的索引,用于直接定位启动模块</param>
    /// <param name="startupModuleType">启动模块类型</param>
    /// <returns>排好序的模块描述信息</returns>
    /// <exception cref="ModuleLoadException">依赖成环,或启动模块不在列表里时抛出</exception>
    protected virtual List<SnowModuleDescriptor> SortByDependency(
        List<SnowModuleDescriptor> modules,
        IReadOnlyDictionary<Type, SnowModuleDescriptor> modulesByType,
        Type startupModuleType)
    {
        List<SnowModuleDescriptor> sortedModules = new List<SnowModuleDescriptor>(modules.Count);
        Dictionary<Type, int> states = new Dictionary<Type, int>(modules.Count);
        HashSet<SnowModuleDescriptor> addedModules = new HashSet<SnowModuleDescriptor>(modules.Count);
        List<Type> path = new List<Type>();

        foreach (SnowModuleDescriptor module in modules)
        {
            Visit(module, sortedModules, addedModules, states, path);
        }

        if (!modulesByType.TryGetValue(startupModuleType, out SnowModuleDescriptor? startupModule))
        {
            throw new ModuleLoadException("启动模块不在已加载的模块列表里: " + startupModuleType.AssemblyQualifiedName);
        }

        sortedModules.Remove(startupModule);
        sortedModules.Add(startupModule);

        return sortedModules;
    }

    private static void Visit(
        SnowModuleDescriptor module,
        List<SnowModuleDescriptor> sortedModules,
        HashSet<SnowModuleDescriptor> addedModules,
        Dictionary<Type, int> states,
        List<Type> path)
    {
        // 1 = 正在访问(出现在当前路径上), 2 = 已完成
        if (states.TryGetValue(module.Type, out int state))
        {
            if (state == 1)
            {
                List<string> cycle = new List<string>(path.Count + 1);

                foreach (Type item in path)
                {
                    cycle.Add(item.Name);
                }

                cycle.Add(module.Type.Name);

                throw new ModuleLoadException("模块依赖出现循环: " + string.Join(" → ", cycle));
            }

            return;
        }

        states[module.Type] = 1;
        path.Add(module.Type);

        foreach (SnowModuleDescriptor dependency in module.Dependencies)
        {
            Visit(dependency, sortedModules, addedModules, states, path);
        }

        path.RemoveAt(path.Count - 1);
        states[module.Type] = 2;

        // states 已保证每个模块只走一次, 这里用 HashSet 免掉列表线性查找
        if (addedModules.Add(module))
        {
            sortedModules.Add(module);
        }
    }
}

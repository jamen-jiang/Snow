using Microsoft.Extensions.DependencyInjection;

namespace Rongia.Modularity;

/// <summary>
/// 模块加载器的默认实现(对应 ABP 的 ModuleLoader):
/// 从启动模块出发收集全部模块类型,建立依赖图后按"被依赖者在前"排序
/// </summary>
public class ModuleLoader : IModuleLoader
{
    /// <inheritdoc />
    public IModuleDescriptor[] LoadModules(IServiceCollection services, Type startupModuleType)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(startupModuleType);

        List<ModuleDescriptor> modules = GetDescriptors(services, startupModuleType);

        List<IModuleDescriptor> sortedModules = SortByDependency(modules, startupModuleType);

        return sortedModules.ToArray();
    }

    private List<ModuleDescriptor> GetDescriptors(IServiceCollection services, Type startupModuleType)
    {
        List<ModuleDescriptor> modules = new List<ModuleDescriptor>();

        FillModules(modules, services, startupModuleType);
        SetDependencies(modules);

        return modules;
    }

    /// <summary>
    /// 从启动模块出发收集全部模块类型,逐个创建描述信息
    /// </summary>
    /// <param name="modules">收集结果</param>
    /// <param name="services">服务集合</param>
    /// <param name="startupModuleType">启动模块类型</param>
    protected virtual void FillModules(List<ModuleDescriptor> modules, IServiceCollection services, Type startupModuleType)
    {
        foreach (Type moduleType in ModuleHelper.FindAllModuleTypes(startupModuleType, null))
        {
            modules.Add(CreateModuleDescriptor(services, moduleType));
        }
    }

    /// <summary>
    /// 为每个模块解析出直接依赖的描述信息
    /// </summary>
    /// <param name="modules">全部模块描述信息</param>
    protected virtual void SetDependencies(List<ModuleDescriptor> modules)
    {
        foreach (ModuleDescriptor module in modules)
        {
            SetDependencies(modules, module);
        }
    }

    /// <summary>
    /// 为单个模块解析出直接依赖的描述信息
    /// </summary>
    /// <param name="modules">全部模块描述信息</param>
    /// <param name="module">待处理的模块</param>
    /// <exception cref="ModuleLoadException">依赖的模块不在已加载的模块列表里</exception>
    protected virtual void SetDependencies(List<ModuleDescriptor> modules, ModuleDescriptor module)
    {
        foreach (Type dependedModuleType in ModuleHelper.FindDependedModuleTypes(module.Type))
        {
            ModuleDescriptor? dependedModule = modules.FirstOrDefault(candidate => candidate.Type == dependedModuleType);

            if (dependedModule == null)
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
    /// <param name="isLoadedAsPlugIn">是否以插件方式加载</param>
    /// <returns>模块描述信息</returns>
    protected virtual ModuleDescriptor CreateModuleDescriptor(IServiceCollection services, Type moduleType, bool isLoadedAsPlugIn = false)
    {
        return new ModuleDescriptor(moduleType, CreateAndRegisterModule(services, moduleType), isLoadedAsPlugIn);
    }

    /// <summary>
    /// 实例化模块并以自身类型注册为单例
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="moduleType">模块类型</param>
    /// <returns>模块实例</returns>
    protected virtual IModule CreateAndRegisterModule(IServiceCollection services, Type moduleType)
    {
        IModule module = ModuleHelper.CreateInstance(moduleType);

        services.AddSingleton(moduleType, module);

        return module;
    }

    /// <summary>
    /// 按依赖关系拓扑排序,被依赖的模块排在前,启动模块固定排最后
    /// </summary>
    /// <param name="modules">全部模块描述信息</param>
    /// <param name="startupModuleType">启动模块类型</param>
    /// <returns>排好序的模块描述信息</returns>
    /// <exception cref="ModuleLoadException">依赖成环时抛出</exception>
    protected virtual List<IModuleDescriptor> SortByDependency(List<ModuleDescriptor> modules, Type startupModuleType)
    {
        List<IModuleDescriptor> sortedModules = new List<IModuleDescriptor>(modules.Count);
        Dictionary<Type, int> states = new Dictionary<Type, int>();
        List<Type> path = new List<Type>();

        foreach (ModuleDescriptor module in modules)
        {
            Visit(module, sortedModules, states, path);
        }

        ModuleDescriptor startupModule = modules.First(module => module.Type == startupModuleType);

        sortedModules.Remove(startupModule);
        sortedModules.Add(startupModule);

        return sortedModules;
    }

    private static void Visit(IModuleDescriptor module, List<IModuleDescriptor> sortedModules, Dictionary<Type, int> states, List<Type> path)
    {
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

        foreach (IModuleDescriptor dependency in module.Dependencies)
        {
            Visit(dependency, sortedModules, states, path);
        }

        path.RemoveAt(path.Count - 1);
        states[module.Type] = 2;

        if (!sortedModules.Contains(module))
        {
            sortedModules.Add(module);
        }
    }
}

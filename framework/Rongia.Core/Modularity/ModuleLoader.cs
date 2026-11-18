namespace Rongia.Modularity;

/// <summary>
/// 模块加载器:从启动模块出发递归解析 DependsOn 声明的依赖,
/// 做去重与循环检测,最后按"被依赖者在前"的顺序返回模块列表
/// </summary>
public sealed class ModuleLoader
{
    /// <summary>
    /// 加载启动模块及其全部依赖
    /// </summary>
    /// <param name="startupModuleType">启动模块类型,必须继承 Module</param>
    /// <returns>按加载顺序排列的模块描述信息</returns>
    /// <exception cref="ModuleLoadException">类型非法、模块抽象或依赖成环时抛出</exception>
    public IReadOnlyList<ModuleDescriptor> LoadModules(Type startupModuleType)
    {
        ArgumentNullException.ThrowIfNull(startupModuleType);

        ValidateModuleType(startupModuleType, "启动模块");

        List<Type> ordered = new List<Type>();
        Dictionary<Type, int> states = new Dictionary<Type, int>();
        List<Type> path = new List<Type>();

        Visit(startupModuleType, ordered, states, path);

        List<ModuleDescriptor> modules = new List<ModuleDescriptor>(ordered.Count);
        foreach (Type moduleType in ordered)
        {
            Module instance = CreateInstance(moduleType);
            modules.Add(new ModuleDescriptor(moduleType, instance, GetDependedModuleTypes(moduleType)));
        }

        return modules;
    }

    private static void Visit(Type moduleType, List<Type> ordered, Dictionary<Type, int> states, List<Type> path)
    {
        if (states.TryGetValue(moduleType, out int state))
        {
            if (state == 1)
            {
                List<string> cycle = new List<string>(path.Count + 1);
                foreach (Type item in path)
                {
                    cycle.Add(item.Name);
                }

                cycle.Add(moduleType.Name);
                throw new ModuleLoadException("模块依赖出现循环: " + string.Join(" → ", cycle));
            }

            return;
        }

        ValidateModuleType(moduleType, "依赖模块");

        states[moduleType] = 1;
        path.Add(moduleType);

        foreach (Type dependency in GetDependedModuleTypes(moduleType))
        {
            Visit(dependency, ordered, states, path);
        }

        path.RemoveAt(path.Count - 1);
        states[moduleType] = 2;
        ordered.Add(moduleType);
    }

    private static Type[] GetDependedModuleTypes(Type moduleType)
    {
        object[] attributes = moduleType.GetCustomAttributes(typeof(DependsOnAttribute), true);

        if (attributes.Length == 0)
        {
            return Type.EmptyTypes;
        }

        List<Type> dependencies = new List<Type>();

        foreach (object attribute in attributes)
        {
            foreach (Type dependency in ((DependsOnAttribute)attribute).DependedModuleTypes)
            {
                if (dependency != null && !dependencies.Contains(dependency))
                {
                    dependencies.Add(dependency);
                }
            }
        }

        return dependencies.ToArray();
    }

    private static void ValidateModuleType(Type moduleType, string role)
    {
        if (!typeof(Module).IsAssignableFrom(moduleType))
        {
            throw new ModuleLoadException(role + " " + moduleType.FullName + " 未继承 Module");
        }

        if (moduleType.IsAbstract)
        {
            throw new ModuleLoadException(role + " " + moduleType.FullName + " 是抽象类型,无法实例化");
        }
    }

    private static Module CreateInstance(Type moduleType)
    {
        try
        {
            return (Module)Activator.CreateInstance(moduleType)!;
        }
        catch (Exception exception)
        {
            throw new ModuleLoadException("模块 " + moduleType.FullName + " 实例化失败,需要提供公共无参构造器", exception);
        }
    }
}
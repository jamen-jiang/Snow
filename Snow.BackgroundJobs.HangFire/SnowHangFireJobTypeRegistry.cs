using System.Reflection;
using Snow.Modularity;

namespace Snow.BackgroundJobs.HangFire;

/// <summary>
/// 作业类型表: 把 beanName(类名/全名/程序集限定名)映射到已加载模块程序集内的具体类型
/// <para>端口只传字符串, 调度框架回放时需要靠这张表把名字还原成类型</para>
/// </summary>
public sealed class SnowHangFireJobTypeRegistry
{
    private readonly Dictionary<string, Type> _types = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 构造器: 扫描模块清单里的全部程序集
    /// </summary>
    /// <param name="moduleContainer">模块清单</param>
    public SnowHangFireJobTypeRegistry(IModuleContainer moduleContainer)
    {
        ArgumentNullException.ThrowIfNull(moduleContainer);

        Assembly[] assemblies = moduleContainer.Modules
            .SelectMany(module => module.AllAssemblies)
            .Distinct()
            .ToArray();

        foreach (Assembly assembly in assemblies)
        {
            foreach (Type type in GetLoadableTypes(assembly))
            {
                if (!type.IsClass || type.IsAbstract || !type.IsVisible)
                {
                    continue;
                }

                Add(type.Name, type);
                Add(type.FullName, type);
                Add(type.AssemblyQualifiedName, type);
                Add(type.Name + ", " + assembly.GetName().Name, type);
            }
        }
    }

    /// <summary>
    /// 已收录的类型个数
    /// </summary>
    public int Count => _types.Count;

    /// <summary>
    /// 按名字取类型
    /// </summary>
    /// <param name="beanName">类名/全名/程序集限定名</param>
    /// <returns>具体类型</returns>
    public Type Resolve(string beanName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(beanName);

        if (_types.TryGetValue(beanName.Trim(), out Type? type))
        {
            return type;
        }

        throw new InvalidOperationException($"找不到名为 {beanName} 的作业类型, 请确认该类型位于已加载模块的程序集内且不是抽象类型。");
    }

    private void Add(string? key, Type type)
    {
        if (!string.IsNullOrWhiteSpace(key))
        {
            _types.TryAdd(key, type);
        }
    }

    private static Type[] GetLoadableTypes(Assembly assembly)
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

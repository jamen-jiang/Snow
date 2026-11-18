using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;

namespace Rongia.DependencyInjection;

/// <summary>
/// 约定注册器基类:负责程序集扫描、候选类型过滤与重复扫描去重,具体登记规则由子类给出
/// </summary>
public abstract class ConventionalRegistrarBase : IConventionalRegistrar
{
    private readonly HashSet<Type> _processedTypes = new HashSet<Type>();

    /// <summary>
    /// 扫描整个程序集并注册(同一注册器实例内不会重复处理同一个类型)
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="assembly">目标程序集</param>
    public void AddAssembly(IServiceCollection services, Assembly assembly)
    {
        foreach (Type type in GetCandidateTypes(assembly))
        {
            if (!_processedTypes.Add(type))
            {
                continue;
            }

            AddType(services, type);
        }
    }

    /// <summary>
    /// 注册指定的多个类型
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="types">类型列表</param>
    public void AddTypes(IServiceCollection services, params Type[] types)
    {
        foreach (Type type in types)
        {
            if (type == null || type.IsInterface || type.IsAbstract)
            {
                continue;
            }

            if (!_processedTypes.Add(type))
            {
                continue;
            }

            AddType(services, type);
        }
    }

    /// <summary>
    /// 注册单个类型,由子类给出登记规则
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="type">类型</param>
    public abstract void AddType(IServiceCollection services, Type type);

    /// <summary>
    /// 获取程序集中可作为注册候选的类型;类型加载失败时退化为已成功加载的部分
    /// </summary>
    /// <param name="assembly">目标程序集</param>
    /// <returns>候选类型数组</returns>
    protected virtual Type[] GetCandidateTypes(Assembly assembly)
    {
        Type[] types;

        try
        {
            types = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            types = exception.Types.Where(type => type != null).Select(type => type!).ToArray();
        }

        return types.Where(IsCandidate).ToArray();
    }

    private static bool IsCandidate(Type type)
    {
        if (!type.IsClass || type.IsAbstract)
        {
            return false;
        }

        if (type.IsDefined(typeof(CompilerGeneratedAttribute), false))
        {
            return false;
        }

        if (type.Name.IndexOf('<') >= 0)
        {
            return false;
        }

        if (type.IsDefined(typeof(DisableConventionalRegistrationAttribute), true))
        {
            return false;
        }

        if (type.IsNested && !type.IsNestedPublic)
        {
            return false;
        }

        return true;
    }
}

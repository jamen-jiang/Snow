using System.Reflection;

namespace Rongia.Modularity;

/// <summary>
/// 声明模块额外参与约定扫描的程序集(对应 ABP 的 AdditionalAssemblyAttribute):
/// 模块主程序集之外,还有别的程序集需要被约定注册扫描时使用
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public class AdditionalAssemblyAttribute : Attribute
{
    /// <summary>
    /// 额外的程序集
    /// </summary>
    public Assembly[] Assemblies { get; }

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="assemblies">额外的程序集</param>
    public AdditionalAssemblyAttribute(params Assembly[]? assemblies)
    {
        Assemblies = assemblies ?? Array.Empty<Assembly>();
    }
}

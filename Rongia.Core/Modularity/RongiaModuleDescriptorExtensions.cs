using System.Reflection;

namespace Rongia.Modularity;

/// <summary>
/// 模块描述信息的扩展方法(对应 ABP 的 AbpModuleDescriptorExtensions)
/// </summary>
public static class RongiaModuleDescriptorExtensions
{
    /// <summary>
    /// 取模块通过 <see cref="AdditionalAssemblyAttribute"/> 额外声明的程序集,
    /// 不含模块自身所在的程序集
    /// </summary>
    /// <param name="module">模块描述信息</param>
    /// <returns>额外程序集数组, 没有时返回空数组</returns>
    public static Assembly[] GetAdditionalAssemblies(this RongiaModuleDescriptor module)
    {
        ArgumentNullException.ThrowIfNull(module);

        return module.AllAssemblies.Length <= 1
            ? []
            : module.AllAssemblies.Where(assembly => assembly != module.Assembly).ToArray();
    }
}
